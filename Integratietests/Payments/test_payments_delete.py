import pytest
import requests
import uuid
import random
import string

# --- SETUP HELPER ---


def setup_payment_transaction(base_url, session_token):
    """
    Maakt ParkingLot -> Vehicle -> Start Session -> Stop Session
    Geeft de Transaction ID terug.
    """
    headers = {"Authorization": session_token,
               "Content-Type": "application/json"}

    # 1. Maak unieke ParkingLot (Admin rechten nodig)
    lot_payload = {
        "name": f"DelTest_{uuid.uuid4().hex[:6]}",
        "location": "Delete Test Location",
        "address": "Delete Lane 1",
        "capacity": 10,
        "tariff": 5.00,
        "dayTariff": 30.00,
        "coordinates": {"latitude": 52.0, "longitude": 5.0}
    }
    lot_resp = requests.post(
        f"{base_url}parkinglots", json=lot_payload, headers=headers, verify=False)
    lot_resp.raise_for_status()
    lot_id = lot_resp.json()["id"]

    # 2. Maak uniek Vehicle
    # Dutch license plate format: XX-NN-XX (2 letters, 2 numbers, 2 letters)
    chars = "".join(random.choices(string.ascii_uppercase, k=2))
    nums = "".join(random.choices(string.digits, k=2))
    chars2 = "".join(random.choices(string.ascii_uppercase, k=2))
    license_plate = f"{chars}-{nums}-{chars2}"
    veh_payload = {"licensePlate": license_plate,
                   "vehicleType": "PassengerCar", "brand": "Test", "model": "Test"}
    veh_resp = requests.post(
        f"{base_url}vehicle", json=veh_payload, headers=headers, verify=False)
    veh_resp.raise_for_status()
    vehicle_id = veh_resp.json()["id"]

    # 3. Start Sessie
    requests.post(f"{base_url}parkinglots/start-session", json={
        "vehicleId": vehicle_id, "parkingLotId": lot_id
    }, headers=headers, verify=False).raise_for_status()

    # 4. Stop Sessie -> Genereert Payment
    stop_resp = requests.post(f"{base_url}parkinglots/stop-session", json={
        "licensePlate": license_plate
    }, headers=headers, verify=False)
    stop_resp.raise_for_status()

    # Haal transactie ID op uit response
    return stop_resp.json()["payment"]["transaction"]

# --- TESTS ---


def assert_is_json(response: requests.Response) -> None:
    assert response.headers.get(
        "Content-Type", "").startswith("application/json")


def test_admin_delete_payment_success(admin_session):
    """Maakt live een betaling aan en verwijdert deze direct."""
    base_url = admin_session["url"]
    admin_token = admin_session["session_token"]

    # STAP 1: Genereer data
    try:
        transaction_id = setup_payment_transaction(base_url, admin_token)
    except requests.exceptions.HTTPError as e:
        pytest.fail(
            f"Setup faalde (waarschijnlijk rechten of model validatie): {e}")

    # STAP 2: Voer de test uit (Delete)
    url = base_url + f"payments/{transaction_id}"
    headers = {"Authorization": admin_token}

    response = requests.delete(url, headers=headers, verify=False)

    # Service geeft (true, null, null) -> Controller geeft NoContent (204)
    assert response.status_code == 204


def test_admin_delete_payment_not_found(admin_session):
    # Hier hoeven we niks aan te maken, want we willen juist dat het NIET bestaat
    transaction_id = str(uuid.uuid4())  # Gewoon een random GUID genereren

    url = admin_session["url"] + f"payments/{transaction_id}"
    headers = {"Authorization": admin_session["session_token"]}

    response = requests.delete(url, headers=headers, verify=False)

    assert response.status_code == 404
