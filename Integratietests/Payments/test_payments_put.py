import pytest
import requests
import uuid
import random
import string

# --- HELPER FUNCTIES ---


def generate_license_plate():
    """Genereer een random Nederlands kentaken (XX-NN-XX format)."""
    chars = "".join(random.choices(string.ascii_uppercase, k=2))
    nums = "".join(random.choices(string.digits, k=2))
    chars2 = "".join(random.choices(string.ascii_uppercase, k=2))
    return f"{chars}-{nums}-{chars2}"


def create_setup_data(base_url, session_token):
    """
    Maakt een ParkingLot en een Vehicle aan via de API en returnt de ID's.
    LET OP: Voor het aanmaken van een ParkingLot is een ADMIN token nodig.
    We nemen hier aan dat 'session_token' voldoende rechten heeft, 
    of je moet hier een apart admin-token gebruiken.
    """
    headers = {"Authorization": session_token,
               "Content-Type": "application/json"}

    # 1. Maak unieke ParkingLot aan
    # Payload gebaseerd op ParkingLotRequestDto
    lot_name = f"TestLot_{uuid.uuid4().hex[:8]}"
    lot_payload = {
        "name": lot_name,
        "location": "Integration Test Location",
        "address": "Integration Test Lane 1",
        "capacity": 50,
        "tariff": 2.50,
        "dayTariff": 15.00,
        "coordinates": {"latitude": 52.0, "longitude": 5.0}
    }

    # POST /parkinglots (Vereist Admin rol in jouw C# controller)
    lot_resp = requests.post(
        f"{base_url}parkinglots", json=lot_payload, headers=headers, verify=False)

    if lot_resp.status_code == 403:
        pytest.fail(
            "De gebruiker in deze test heeft geen Admin-rechten om een ParkingLot aan te maken.")
    lot_resp.raise_for_status()
    parking_lot_id = lot_resp.json()["id"]

    # 2. Maak uniek Vehicle aan
    license_plate = generate_license_plate()
    vehicle_payload = {
        "licensePlate": license_plate,
        "vehicleType": "PassengerCar",  # Pas aan naar jouw enum/string values
        "brand": "TestBrand",
        "model": "TestModel"
    }

    # POST /vehicle (Controller route is [HttpPost("vehicle")])
    veh_resp = requests.post(
        f"{base_url}vehicle", json=vehicle_payload, headers=headers, verify=False)
    veh_resp.raise_for_status()
    vehicle_data = veh_resp.json()

    return {
        "parkingLotId": parking_lot_id,
        "vehicleId": vehicle_data["id"],
        "licensePlate": vehicle_data["licensePlate"]
    }

# --- TESTS ---


def test_complete_payment_success_via_session_flow(user_session, admin_session):
    """
    Volledige Flow Test: Create Resources > Start > Stop > Complete Betaling
    """
    base_url = user_session["url"]
    headers = {"Authorization": user_session["session_token"]}

    # STAP 0: Setup Resources (ParkingLot & Vehicle)
    # Dit zorgt dat de test altijd werkt, ongeacht de staat van de database.
    test_data = create_setup_data(base_url, admin_session["session_token"])

    parking_lot_id = test_data["parkingLotId"]
    vehicle_id = test_data["vehicleId"]
    license_plate = test_data["licensePlate"]

    print(
        f"\nTest setup complete: LotID={parking_lot_id}, VehicleID={vehicle_id}, Plate={license_plate}")

    # STAP 1: Start Sessie
    start_url = base_url + "parkinglots/start-session"
    start_payload = {
        "vehicleId": vehicle_id,
        "parkingLotId": parking_lot_id
    }

    start_resp = requests.post(
        start_url, json=start_payload, headers=headers, verify=False)
    start_resp.raise_for_status()

    # STAP 2: Stop Sessie (Dit genereert de Payment Initiatie DTO)
    stop_url = base_url + "parkinglots/stop-session"
    stop_payload = {"licensePlate": license_plate}

    stop_resp = requests.post(
        stop_url, json=stop_payload, headers=headers, verify=False)
    stop_resp.raise_for_status()

    stop_body = stop_resp.json()

    # Check of de structuur klopt (debugging)
    assert "payment" in stop_body, "Response mist 'payment' object"
    transaction_id = stop_body["payment"]["transaction"]

    # Hier simuleren we de hash validatie.
    # In een echte integratietest zou je hier de werkelijke logica moeten nabootsen
    # of een backdoor/mock gebruiken als je de hash niet client-side kunt berekenen.
    CORRECT_VALIDATION_HASH = "mocked-hash-uit-database"

    # STAP 3: Complete de Betaling
    complete_url = base_url + f"payments/{transaction_id}"

    complete_payload = {
        "validation": CORRECT_VALIDATION_HASH,
        "t_data": {
            "processorId": f"test_proc_{uuid.uuid4()}",  # Unieke processor ID
            "status": "success"
        }
    }

    complete_resp = requests.put(
        complete_url, json=complete_payload, headers=headers, verify=False)

    assert complete_resp.status_code == 200
    assert complete_resp.json().get("status") == "Paid"


def test_complete_payment_validation_failed(user_session, admin_session):
    """Test 401 als de hash (validation) niet overeenkomt."""

    # We moeten eerst een geldige transactie hebben om op te testen,
    # anders krijgen we waarschijnlijk een 404 Not Found op de transactie ID ipv een 401.
    # We hergebruiken de setup logica.

    base_url = user_session["url"]
    headers = {"Authorization": user_session["session_token"]}

    # Setup en start/stop flow om een geldig transactie ID te krijgen
    test_data = create_setup_data(base_url, admin_session["session_token"])

    # Start
    requests.post(base_url + "parkinglots/start-session", json={
        "vehicleId": test_data["vehicleId"],
        "parkingLotId": test_data["parkingLotId"]
    }, headers=headers, verify=False).raise_for_status()

    # Stop
    stop_resp = requests.post(base_url + "parkinglots/stop-session", json={
        "licensePlate": test_data["licensePlate"]
    }, headers=headers, verify=False)

    transaction_id = stop_resp.json()["payment"]["transaction"]

    # Nu testen we de faling
    url = base_url + f"payments/{transaction_id}"

    payload = {
        "validation": "verkeerde-hash",
        "t_data": {"info": "test"}
    }

    response = requests.put(url, json=payload, headers=headers, verify=False)

    assert response.status_code == 401
    assert "Validation failed" in response.text
