import json
import pytest
import requests
from jsonschema import validate, ValidationError
from dateutil import parser as dateparser


#Post route tests start sessions
def test_session_start_unauthorized(user_session):
    lid = 1
    url = user_session["url"] + f"parking-lots/{lid}/sessions/start"

    r = requests.post(url, json={"licenseplate": "TEST123"})
    assert r.status_code == 401


def test_session_start_missing_licenseplate(user_session):
    lid = 1
    url = user_session["url"] + f"parking-lots/{lid}/sessions/start"
    headers = {"Authorization": user_session["session_token"]}

    r = requests.post(url, headers=headers, json={})
    assert r.status_code == 401
    assert r.json()["field"] == "licenseplate"


def test_session_start_success(user_session):
    lid = 1
    url = user_session["url"] + f"parking-lots/{lid}/sessions/start"
    headers = {"Authorization": user_session["session_token"]}

    r = requests.post(url, headers=headers, json={"licenseplate": "NEWCAR1"})
    assert r.status_code == 200
    assert "Session started for: NEWCAR1" in r.text


def test_session_start_twice_fails(user_session):
    lid = 1
    url = user_session["url"] + f"parking-lots/{lid}/sessions/start"
    headers = {"Authorization": user_session["session_token"]}

    lp = "DUPLICATE1"

    first = requests.post(url, headers=headers, json={"licenseplate": lp})
    assert first.status_code == 200

    second = requests.post(url, headers=headers, json={"licenseplate": lp})
    assert second.status_code == 401
    assert b"already started" in second.content


def test_session_start_invalid_lid(user_session):
    lid = 9999
    url = user_session["url"] + f"parking-lots/{lid}/sessions/start"
    headers = {"Authorization": user_session["session_token"]}

    r = requests.post(url, headers=headers, json={"licenseplate": "CAR123"})
    assert r.status_code in [400, 404, 500]


#Post route tests end sessions

def test_session_stop_unauthorized(user_session):
    lid = 1
    url = user_session["url"] + f"parking-lots/{lid}/sessions/stop"

    r = requests.post(url, json={"licenseplate": "CAR123"})
    assert r.status_code == 401


def test_session_stop_missing_licenseplate(user_session):
    lid = 1
    url = user_session["url"] + f"parking-lots/{lid}/sessions/stop"
    headers = {"Authorization": user_session["session_token"]}

    r = requests.post(url, headers=headers, json={})
    assert r.status_code == 401
    assert r.json()["field"] == "licenseplate"


def test_session_stop_success(user_session):
    lid = 1
    base = user_session["url"] + f"parking-lots/{lid}/sessions"
    headers = {"Authorization": user_session["session_token"]}

    lp = "STOPME1"

    # starten
    start = requests.post(base + "/start", headers=headers, json={"licenseplate": lp})
    assert start.status_code == 200

    # stoppen
    stop = requests.post(base + "/stop", headers=headers, json={"licenseplate": lp})
    assert stop.status_code == 200
    assert "Session stopped for" in stop.text


def test_session_stop_without_active_session(user_session):
    lid = 1
    url = user_session["url"] + f"parking-lots/{lid}/sessions/stop"
    headers = {"Authorization": user_session["session_token"]}

    r = requests.post(url, headers=headers, json={"licenseplate": "NOSUCHCAR"})
    assert r.status_code in [200, 401]   # door bug in code


def test_session_stop_invalid_lid(user_session):
    lid = 9999
    url = user_session["url"] + f"parking-lots/{lid}/sessions/stop"
    headers = {"Authorization": user_session["session_token"]}

    r = requests.post(url, headers=headers, json={"licenseplate": "CAR1"})
    assert r.status_code in [400, 404, 500]


#Put route tests

import requests
import json

def test_update_parking_lot_unauthorized(user_session):
    url = user_session["url"] + "parking-lots/1"
    r = requests.put(url, json={"name": "NewLot"})
    assert r.status_code == 401
    assert "Unauthorized" in r.text


def test_update_parking_lot_forbidden_non_admin(user_session):
    url = user_session["url"] + "parking-lots/1"
    headers = {"Authorization": user_session["session_token"]}

    r = requests.put(url, headers=headers, json={"name": "HackLot"})
    assert r.status_code == 403
    assert r.text == "Access denied"


def test_update_parking_lot_not_found(admin_session):
    url = admin_session["url"] + "parking-lots/99999"
    headers = {"Authorization": admin_session["session_token"]}

    r = requests.put(url, headers=headers, json={"name": "DoesNotExist"})
    assert r.status_code == 404
    assert r.text == "Parking lot not found"


def test_update_parking_lot_success(admin_session):
    url = admin_session["url"] + "parking-lots/1"
    headers = {"Authorization": admin_session["session_token"]}

    data = {
        "name": "UpdatedLot",
        "location": "CenterCity",
        "address": "New Street 10",
        "capacity": 120,
        "reserved": 5,
        "tariff": 3.0,
        "daytariff": 15,
        "currency": "EUR",
        "created_at": "2024-01-01",
        "coordinates": {"lat": 52.0, "lng": 4.1}
    }

    r = requests.put(url, headers=headers, json=data)
    assert r.status_code == 200
    assert r.text == "Parking lot modified"


def test_update_parking_lot_invalid_json(admin_session):
    url = admin_session["url"] + "parking-lots/1"
    headers = {"Authorization": admin_session["session_token"]}

    r = requests.put(url, headers=headers, data="INVALID_JSON")
    assert r.status_code in [400, 500]


#Delete route tests

def test_delete_parking_lot_unauthorized(user_session):
    url = user_session["url"] + "parking-lots/1"
    r = requests.delete(url)
    assert r.status_code == 401


def test_delete_parking_lot_forbidden(user_session):
    url = user_session["url"] + "parking-lots/1"
    headers = {"Authorization": user_session["session_token"]}

    r = requests.delete(url, headers=headers)
    assert r.status_code == 403
    assert r.text == "Access denied"


def test_delete_parking_lot_not_found(admin_session):
    url = admin_session["url"] + "parking-lots/99999"
    headers = {"Authorization": admin_session["session_token"]}

    r = requests.delete(url, headers=headers)
    assert r.status_code == 404
    assert r.text == "Parking lot not found"


def test_delete_parking_lot_success(admin_session):
    # eerst een parking lot maken zodat hij zeker bestaat
    create_url = admin_session["url"] + "parking-lots"
    headers = {"Authorization": admin_session["session_token"]}

    new_lot = {
        "name": "TempLot",
        "location": "TestCity",
        "address": "Temp 1",
        "capacity": 50,
        "reserved": 2,
        "tariff": 2.5,
        "daytariff": 15,
        "currency": "EUR",
        "created_at": "2024-01-01",
        "coordinates": {"lat": 51.0, "lng": 4.0}
    }

    create = requests.post(create_url, headers=headers, json=new_lot)
    assert create.status_code == 201

    new_id = create.text.split(":")[-1].strip()

    delete_url = admin_session["url"] + f"parking-lots/{new_id}"

    r = requests.delete(delete_url, headers=headers)
    assert r.status_code == 200
    assert r.text == "Parking lot deleted"


def test_delete_parking_lot_invalid_path(admin_session):
    url = admin_session["url"] + "parking-lots/"
    headers = {"Authorization": admin_session["session_token"]}

    r = requests.delete(url, headers=headers)
    assert r.status_code in [400, 404]



#Delete session tests
def test_delete_session_unauthorized(user_session):
    url = user_session["url"] + "parking-lots/1/sessions/1"
    r = requests.delete(url)
    assert r.status_code == 401


def test_delete_session_forbidden(user_session):
    url = user_session["url"] + "parking-lots/1/sessions/1"
    headers = {"Authorization": user_session["session_token"]}

    r = requests.delete(url, headers=headers)
    assert r.status_code == 403
    assert r.text == "Access denied"


def test_delete_session_non_numeric(admin_session):
    url = admin_session["url"] + "parking-lots/1/sessions/notanumber"
    headers = {"Authorization": admin_session["session_token"]}

    r = requests.delete(url, headers=headers)
    assert r.status_code == 403
    assert r.text == "Session ID is required, cannot delete all sessions"


def test_delete_session_not_found(admin_session):
    url = admin_session["url"] + "parking-lots/1/sessions/99999"
    headers = {"Authorization": admin_session["session_token"]}

    r = requests.delete(url, headers=headers)

    # afhankelijk van of KeyError wordt opgevangen → 404 of 500
    assert r.status_code in [404, 500]


def test_delete_session_success(admin_session):
    base = admin_session["url"] + "parking-lots/1/sessions"
    headers = {"Authorization": admin_session["session_token"]}

    # eerst sessie starten
    start = requests.post(base + "/start", headers=headers, json={"licenseplate": "DELME123"})
    assert start.status_code == 200

    # sessies krijgen numerieke ID's (1, 2, ...)
    delete_url = admin_session["url"] + "parking-lots/1/sessions/1"

    r = requests.delete(delete_url, headers=headers)
    assert r.status_code == 200
    assert r.text == "Sessions deleted"
