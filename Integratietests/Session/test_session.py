import pytest
import requests
import uuid
import random
import string
import time
from datetime import datetime, timedelta, timezone

# --- HELPERS (Uitgebreid) ---


def generate_license_plate():
    """Genereer een random Nederlands kenteken om conflicten te voorkomen (NL-LL-NNNN)."""
    chars = "".join(random.choices(string.ascii_uppercase, k=2))
    nums = "".join(random.choices(string.digits, k=4))
    # Eerste 3 combinaties van NL zijn LL-NN-NN, NN-LL-NN, NN-NN-LL (simpelere NL-variant)
    # Ik gebruik hier een simpele, unieke format om conflicten te vermijden.
    return f"{chars}-{random.choices(string.digits, k=2)[0]}{random.choices(string.digits, k=2)[0]}-{random.choices(string.ascii_uppercase, k=2)[0]}{random.choices(string.ascii_uppercase, k=2)[0]}"


def create_setup_data(base_url, user_token, admin_token):
    """
    Maakt een ParkingLot en een Vehicle aan via de API en returnt de ID's.
    Vereist een ADMIN token voor ParkingLot.
    """
    admin_headers = {"Authorization": admin_token,
                     "Content-Type": "application/json"}
    user_headers = {"Authorization": user_token,
                    "Content-Type": "application/json"}

    # 1. Maak unieke ParkingLot aan (Admin vereist)
    lot_name = f"TestLot_{uuid.uuid4().hex[:8]}"
    lot_payload = {
        "name": lot_name,
        "location": "Integration Test Location",
        "address": "Integration Test Lane 1",
        "capacity": 50,
        "tariff": 2.50,
        "dayTariff": 15.0,
        "coordinates": {"latitude": 52.0, "longitude": 5.0}
    }

    # POST /parkinglots (Admin route)
    lot_resp = requests.post(
        f"{base_url}parkinglots", json=lot_payload, headers=admin_headers, verify=False)

    if lot_resp.status_code == 403:
        pytest.fail(
            "De Admin-token is ongeldig voor het aanmaken van een ParkingLot.")
    lot_resp.raise_for_status()
    parking_lot_id = lot_resp.json()["id"]

    # 2. Maak uniek Vehicle aan (User token vereist, aangenomen route /vehicle)
    license_plate = generate_license_plate()
    vehicle_payload = {
        "licensePlate": license_plate,
        "vehicleType": "PassengerCar",
        "brand": "TestBrand",
        "model": "TestModel"
    }

    # POST /vehicle (Aangenomen dat dit de route is voor aanmaken)
    veh_resp = requests.post(
        f"{base_url}vehicle", json=vehicle_payload, headers=user_headers, verify=False)
    veh_resp.raise_for_status()
    vehicle_data = veh_resp.json()

    return {
        "parkingLotId": parking_lot_id,
        "vehicleId": vehicle_data["id"],
        "licensePlate": vehicle_data["licensePlate"]
    }

# --- TESTS ---


def test_session_lifecycle_success_user_flow(user_session, admin_session):
    """
    Volledige flow voor een standaard gebruiker:
    Start -> Stop op basis van kenteken -> Get Sessions
    """
    base_url = user_session["url"]
    user_token = user_session["session_token"]
    admin_token = admin_session["session_token"]
    user_headers = {"Authorization": user_token,
                    "Content-Type": "application/json"}

    # STAP 0: Setup Resources (ParkingLot & Vehicle)
    test_data = create_setup_data(base_url, user_token, admin_token)
    parking_lot_id = test_data["parkingLotId"]
    vehicle_id = test_data["vehicleId"]
    license_plate = test_data["licensePlate"]

    # STAP 1: Start Sessie (POST /parkinglots/start-session)
    start_payload = {
        "vehicleId": vehicle_id,
        "parkingLotId": parking_lot_id
    }
    start_url = base_url + "parkinglots/start-session"

    start_resp = requests.post(
        start_url, json=start_payload, headers=user_headers, verify=False)
    start_resp.raise_for_status()
    session_data = start_resp.json()
    session_id = session_data["id"]

    assert session_data["vehicleId"] == vehicle_id
    assert session_data["paymentStatus"] == "unpaid"

    # Wacht 1 seconde om een meetbare duur te garanderen
    time.sleep(1)

    # STAP 2: Stop Sessie met Kenteken (POST /parkinglots/stop-session)
    stop_url = base_url + "parkinglots/stop-session"
    stop_payload = {"licensePlate": license_plate}

    stop_resp = requests.post(
        stop_url, json=stop_payload, headers=user_headers, verify=False)
    stop_resp.raise_for_status()

    stop_body = stop_resp.json()
    assert stop_body["session"]["stopped"] is not None
    assert stop_body["session"]["cost"] > 0
    assert stop_body["payment"]["transaction"] is not None

    # STAP 3: Get Sessies (GET /parkinglots/sessions)
    get_sessions_url = base_url + "parkinglots/sessions"
    get_resp = requests.get(
        get_sessions_url, headers=user_headers, verify=False)
    get_resp.raise_for_status()
    sessions_list = get_resp.json()

    # Controleer of de gestopte sessie in de lijst zit
    stopped_session = next(
        (s for s in sessions_list if s["id"] == session_id), None)
    assert stopped_session is not None
    assert stopped_session["stopped"] is not None


def test_start_session_conflict(user_session, admin_session):
    """Test 409 Conflict: Start een tweede sessie met hetzelfde voertuig."""
    base_url = user_session["url"]
    user_token = user_session["session_token"]
    admin_token = admin_session["session_token"]
    user_headers = {"Authorization": user_token,
                    "Content-Type": "application/json"}

    # STAP 0: Setup Resources
    test_data = create_setup_data(base_url, user_token, admin_token)
    parking_lot_id = test_data["parkingLotId"]
    vehicle_id = test_data["vehicleId"]

    # STAP 1: Start de eerste sessie
    start_payload = {
        "vehicleId": vehicle_id,
        "parkingLotId": parking_lot_id
    }
    start_url = base_url + "parkinglots/start-session"
    requests.post(start_url, json=start_payload,
                  headers=user_headers, verify=False).raise_for_status()

    # STAP 2: Probeer de tweede sessie te starten
    second_start_resp = requests.post(
        start_url, json=start_payload, headers=user_headers, verify=False)

    assert second_start_resp.status_code == 409
    assert "already an active session" in second_start_resp.text


def test_stop_session_not_found(user_session):
    """Test 404 Not Found: Stop een sessie met een onbekend kenteken."""
    base_url = user_session["url"]
    user_headers = {
        "Authorization": user_session["session_token"], "Content-Type": "application/json"}

    # Probeer te stoppen met een kenteken dat niet geassocieerd is met een actieve sessie
    non_existent_plate = generate_license_plate()
    stop_url = base_url + "parkinglots/stop-session"
    stop_payload = {"licensePlate": non_existent_plate}

    stop_resp = requests.post(
        stop_url, json=stop_payload, headers=user_headers, verify=False)

    assert stop_resp.status_code == 404
    assert "not found" in stop_resp.text


def test_get_sessions_only_active(user_session, admin_session):
    """Test GET /parkinglots/sessions?onlyActive=true."""
    base_url = user_session["url"]
    user_token = user_session["session_token"]
    admin_token = admin_session["session_token"]
    user_headers = {"Authorization": user_token,
                    "Content-Type": "application/json"}

    # STAP 0: Setup Resources voor 2 sessies
    data_active = create_setup_data(base_url, user_token, admin_token)
    data_stopped = create_setup_data(base_url, user_token, admin_token)

    # Sessie 1: Actief
    start_url = base_url + "parkinglots/start-session"
    active_payload = {
        "vehicleId": data_active["vehicleId"],
        "parkingLotId": data_active["parkingLotId"]
    }
    active_resp = requests.post(
        start_url, json=active_payload, headers=user_headers, verify=False)
    active_resp.raise_for_status()
    active_session_id = active_resp.json()["id"]

    # Sessie 2: Gestopt
    stopped_payload = {
        "vehicleId": data_stopped["vehicleId"],
        "parkingLotId": data_stopped["parkingLotId"]
    }
    requests.post(start_url, json=stopped_payload,
                  headers=user_headers, verify=False).raise_for_status()

    stop_url = base_url + "parkinglots/stop-session"
    stop_payload = {"licensePlate": data_stopped["licensePlate"]}
    requests.post(stop_url, json=stop_payload,
                  headers=user_headers, verify=False).raise_for_status()

    # Vraag alleen actieve sessies op
    get_url = base_url + "parkinglots/sessions?onlyActive=true"
    get_resp = requests.get(get_url, headers=user_headers, verify=False)
    get_resp.raise_for_status()
    sessions_list = get_resp.json()

    # Controleer of de actieve sessie erin zit en de gestopte sessie niet
    active_exists = any(s["id"] == active_session_id for s in sessions_list)
    stopped_exists = any(s["licensePlate"] ==
                         data_stopped["licensePlate"] for s in sessions_list)

    assert active_exists
    assert not stopped_exists

# --- ADMIN ROUTES ---


def test_admin_stop_session_by_id_success(user_session, admin_session):
    """Test PUT /parkinglots/stop-session/{id:guid} met Admin-token."""
    base_url = user_session["url"]
    user_token = user_session["session_token"]
    admin_token = admin_session["session_token"]
    user_headers = {"Authorization": user_token,
                    "Content-Type": "application/json"}
    admin_headers = {"Authorization": admin_token,
                     "Content-Type": "application/json"}

    # STAP 0: Setup en Start een sessie (als normale gebruiker)
    test_data = create_setup_data(base_url, user_token, admin_token)
    start_payload = {
        "vehicleId": test_data["vehicleId"],
        "parkingLotId": test_data["parkingLotId"]
    }
    start_resp = requests.post(
        base_url + "parkinglots/start-session", json=start_payload, headers=user_headers, verify=False)
    start_resp.raise_for_status()
    session_id = start_resp.json()["id"]

    # STAP 1: Stop de sessie met de Admin route (PUT /parkinglots/stop-session/{id})
    stop_url = base_url + f"parkinglots/stop-session/{session_id}"
    stop_resp = requests.put(stop_url, headers=admin_headers, verify=False)
    stop_resp.raise_for_status()

    stop_body = stop_resp.json()
    assert stop_body["session"]["stopped"] is not None
    assert stop_body["payment"]["transaction"] is not None


def test_admin_stop_session_by_id_not_admin_403(user_session, admin_session):
    """Test 403 Forbidden: Probeer Admin Stop Session te gebruiken met User-token."""
    base_url = user_session["url"]
    user_token = user_session["session_token"]
    admin_token = admin_session["session_token"]
    user_headers = {"Authorization": user_token,
                    "Content-Type": "application/json"}

    # STAP 0: Setup en Start een sessie
    test_data = create_setup_data(base_url, user_token, admin_token)
    start_payload = {
        "vehicleId": test_data["vehicleId"],
        "parkingLotId": test_data["parkingLotId"]
    }
    start_resp = requests.post(
        base_url + "parkinglots/start-session", json=start_payload, headers=user_headers, verify=False)
    start_resp.raise_for_status()
    session_id = start_resp.json()["id"]

    # STAP 1: Probeer te stoppen met User token
    stop_url = base_url + f"parkinglots/stop-session/{session_id}"
    stop_resp = requests.put(stop_url, headers=user_headers, verify=False)

    assert stop_resp.status_code == 403  # Moet 403 Forbidden zijn


def test_admin_cancel_session_success(user_session, admin_session):
    """Test PUT /parkinglots/cancel-session/{id:guid} met Admin-token."""
    base_url = user_session["url"]
    user_token = user_session["session_token"]
    admin_token = admin_session["session_token"]
    user_headers = {"Authorization": user_token,
                    "Content-Type": "application/json"}
    admin_headers = {"Authorization": admin_token,
                     "Content-Type": "application/json"}

    # STAP 0: Setup en Start een sessie
    test_data = create_setup_data(base_url, user_token, admin_token)
    start_payload = {
        "vehicleId": test_data["vehicleId"],
        "parkingLotId": test_data["parkingLotId"]
    }
    start_resp = requests.post(
        base_url + "parkinglots/start-session", json=start_payload, headers=user_headers, verify=False)
    start_resp.raise_for_status()
    session_id = start_resp.json()["id"]

    # STAP 1: Annuleer de sessie (PUT /parkinglots/cancel-session/{id})
    cancel_url = base_url + f"parkinglots/cancel-session/{session_id}"
    cancel_payload = {"reason": "Geannuleerd tijdens integratietest"}
    cancel_resp = requests.put(
        cancel_url, json=cancel_payload, headers=admin_headers, verify=False)
    cancel_resp.raise_for_status()

    cancelled_session = cancel_resp.json()
    assert cancelled_session["isCancelled"] is True
    assert cancelled_session["cancelledAt"] is not None
    # Sessie is gestopt bij annulering als deze nog actief was
    assert cancelled_session["stopped"] is not None


def test_admin_delete_session_success(user_session, admin_session):
    """Test DELETE /parking-lots/{parkingLotId}/sessions/{sessionId} met Admin-token."""
    base_url = user_session["url"]
    user_token = user_session["session_token"]
    admin_token = admin_session["session_token"]
    user_headers = {"Authorization": user_token,
                    "Content-Type": "application/json"}
    admin_headers = {"Authorization": admin_token,
                     "Content-Type": "application/json"}

    # STAP 0: Setup en Start een sessie
    test_data = create_setup_data(base_url, user_token, admin_token)
    parking_lot_id = test_data["parkingLotId"]
    start_payload = {
        "vehicleId": test_data["vehicleId"],
        "parkingLotId": parking_lot_id
    }
    start_resp = requests.post(
        base_url + "parkinglots/start-session", json=start_payload, headers=user_headers, verify=False)
    start_resp.raise_for_status()
    session_id = start_resp.json()["id"]

    # STAP 1: Verwijder de sessie
    delete_url = base_url + \
        f"parkinglots/{parking_lot_id}/sessions/{session_id}"
    delete_resp = requests.delete(
        delete_url, headers=admin_headers, verify=False)

    assert delete_resp.status_code == 204  # No Content bij succesvolle verwijdering

    # STAP 2: Controleer of de sessie echt weg is (GET /get-session-by-id)
    get_url = base_url + f"parkinglots/get-session-by-id?id={session_id}"
    get_resp = requests.get(get_url, headers=admin_headers, verify=False)
    assert get_resp.status_code == 404


def test_admin_refund_session_success(user_session, admin_session):
    """Test POST /parkinglots/refund-session/{id:guid} met Admin-token na annulering."""
    base_url = user_session["url"]
    user_token = user_session["session_token"]
    admin_token = admin_session["session_token"]
    user_headers = {"Authorization": user_token,
                    "Content-Type": "application/json"}
    admin_headers = {"Authorization": admin_token,
                     "Content-Type": "application/json"}

    # STAP 0: Setup, Start en Stop een sessie
    test_data = create_setup_data(base_url, user_token, admin_token)
    start_payload = {
        "vehicleId": test_data["vehicleId"],
        "parkingLotId": test_data["parkingLotId"]
    }
    start_resp = requests.post(
        base_url + "parkinglots/start-session", json=start_payload, headers=user_headers, verify=False)
    session_id = start_resp.json()["id"]

    # Wacht 5 minuten (gesimuleerd) om een refund met 100% (<= 10 min) te garanderen.
    # Aangezien we hier de werkelijke tijd van de service niet kunnen beïnvloeden,
    # moeten we accepteren dat de duur 0 minuten kan zijn of zeer kort.
    time.sleep(1)  # Een seconde is voldoende voor een 'korte' sessie

    # Annuleer sessie (vereist voor refund)
    cancel_url = base_url + f"parkinglots/cancel-session/{session_id}"
    requests.put(cancel_url, json={"reason": "Refund test"},
                 headers=admin_headers, verify=False).raise_for_status()

    # STAP 1: Vraag de terugbetaling aan
    refund_url = base_url + f"parkinglots/refund-session/{session_id}"
    refund_payload = {"iban": "NL91ABNA0417164300"}

    refund_resp = requests.post(
        refund_url, json=refund_payload, headers=admin_headers, verify=False)
    refund_resp.raise_for_status()

    refund_body = refund_resp.json()
    assert refund_body["refunded"] > 0
    # Afhankelijk van de gesimuleerde duur
    assert refund_body["percentage"] in [100.0, 50.0, 0.0]
