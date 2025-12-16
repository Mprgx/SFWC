import pytest
import requests
import uuid
import random
import string

# --- SETUP HELPER ---


def setup_payment_scenario(base_url, admin_token, user_token):
    """
    Maakt resources aan en stopt de sessie.
    admin_token: voor het aanmaken van ParkingLot
    user_token: voor het starten/stoppen van sessie (payment owner)
    Returnt: { "transactionId": guid, "amount": float }
    """
    admin_headers = {"Authorization": admin_token,
                     "Content-Type": "application/json"}
    user_headers = {"Authorization": user_token,
                    "Content-Type": "application/json"}

    # 1. ParkingLot (Admin)
    lot_payload = {
        "name": f"PayTest_{uuid.uuid4().hex[:6]}",
        "location": "Payment Test Location",
        "address": "Payment Lane 1",
        "capacity": 20,
        "tariff": 10.00,
        "dayTariff": 60.00,
        "coordinates": {"latitude": 52.0, "longitude": 5.0}
    }
    lot_resp = requests.post(
        f"{base_url}parkinglots", json=lot_payload, headers=admin_headers, verify=False)
    lot_resp.raise_for_status()
    lot_id = lot_resp.json()["id"]

    # 2. Vehicle (User)
    # Dutch license plate format: XX-NN-XX (2 letters, 2 numbers, 2 letters)
    chars = "".join(random.choices(string.ascii_uppercase, k=2))
    nums = "".join(random.choices(string.digits, k=2))
    chars2 = "".join(random.choices(string.ascii_uppercase, k=2))
    license_plate = f"{chars}-{nums}-{chars2}"
    veh_resp = requests.post(f"{base_url}vehicle", json={
        "licensePlate": license_plate, "vehicleType": "PassengerCar", "brand": "X", "model": "Y"
    }, headers=user_headers, verify=False)
    veh_resp.raise_for_status()
    vehicle_id = veh_resp.json()["id"]

    # 3. Start (User)
    requests.post(f"{base_url}parkinglots/start-session", json={
        "vehicleId": vehicle_id, "parkingLotId": lot_id
    }, headers=user_headers, verify=False).raise_for_status()

    # 4. Stop (User)
    # (Optioneel: voeg hier een time.sleep(1) toe als je systeem seconden telt voor prijs)
    stop_resp = requests.post(f"{base_url}parkinglots/stop-session", json={
        "licensePlate": license_plate
    }, headers=user_headers, verify=False)
    stop_resp.raise_for_status()

    data = stop_resp.json()

    # CHECK: Pas deze keys aan als je JSON structuur anders is!
    payment_info = data.get("payment", {})
    return {
        "transactionId": payment_info.get("transaction"),
        # Zorg dat je API dit veld teruggeeft bij Stop!
        "amount": payment_info.get("amount")
    }

# --- TESTS ---


def assert_is_json(response: requests.Response) -> None:
    assert response.headers.get(
        "Content-Type", "").startswith("application/json")


def test_fulfill_payment_success(user_session, admin_session):
    """
    Happy Path: Haalt echte amount op uit setup en betaalt deze.
    """
    base_url = user_session["url"]
    # Setup uses admin token for parking lot creation, user token for session/payment
    payment_data = setup_payment_scenario(
        base_url, admin_session["session_token"], user_session["session_token"])

    url = base_url + "payments/fulfill"
    headers = {"Authorization": user_session["session_token"]}

    # Payload met de DYNAMISCHE data
    payload = {
        "transaction": payment_data["transactionId"],
        "amount": payment_data["amount"]
    }

    response = requests.post(url, json=payload, headers=headers, verify=False)

    assert response.status_code == 200
    assert_is_json(response)
    assert response.json().get("status") == "paid"


def test_fulfill_payment_amount_mismatch(user_session, admin_session):
    """Test conflict (409) als het bedrag niet klopt."""
    base_url = user_session["url"]
    payment_data = setup_payment_scenario(
        base_url, admin_session["session_token"], user_session["session_token"])

    url = base_url + "payments/fulfill"
    headers = {"Authorization": user_session["session_token"]}

    # We nemen het echte bedrag en tellen er 100 bij op om zeker te zijn van een mismatch
    wrong_amount = (payment_data["amount"] or 0) + 100.50

    payload = {
        "transaction": payment_data["transactionId"],
        "amount": wrong_amount
    }

    response = requests.post(url, json=payload, headers=headers, verify=False)

    assert response.status_code == 409
    assert "Amount mismatch" in response.text


def test_fulfill_payment_not_found(user_session):
    """Test 404 als transactie ID niet bestaat."""
    # Hier is geen setup nodig, we verzinnen een ID
    url = user_session["url"] + "payments/fulfill"
    headers = {"Authorization": user_session["session_token"]}

    payload = {
        "transaction": str(uuid.uuid4()),
        "amount": 10.00
    }

    response = requests.post(url, json=payload, headers=headers, verify=False)

    assert response.status_code == 404
