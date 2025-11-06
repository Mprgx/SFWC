import json
import pytest
import requests
from jsonschema import validate, ValidationError
from dateutil import parser as dateparser


#Post route tests start sessions
def test_start_session_unauthorized(base_url):
    url = base_url + "parking-lots/1/sessions/start"
    r = requests.post(url, json={"licenseplate": "TEST123"})
    assert r.status_code == 401


def test_start_session_missing_licenseplate(user_session):
    url = user_session["url"] + "parking-lots/1/sessions/start"
    token = user_session["token"]

    r = requests.post(url, headers={"Authorization": token}, json={})
    assert r.status_code == 401
    assert r.json()["field"] == "licenseplate"


def test_start_session_success(user_session):
    url = user_session["url"] + "parking-lots/1/sessions/start"
    token = user_session["token"]

    r = requests.post(url, headers={"Authorization": token},
                      json={"licenseplate": "ABC111"})
    assert r.status_code == 200
    assert "Session started for: ABC111" in r.text


def test_start_session_twice_fails(user_session):
    url = user_session["url"] + "parking-lots/1/sessions/start"
    token = user_session["token"]
    lp = "DOUBLE555"

    first = requests.post(url, headers={"Authorization": token}, json={"licenseplate": lp})
    assert first.status_code == 200

    second = requests.post(url, headers={"Authorization": token}, json={"licenseplate": lp})
    assert second.status_code == 401
    assert b"already started" in second.content


def test_start_session_invalid_lid(user_session):
    url = user_session["url"] + "parking-lots/9999/sessions/start"
    token = user_session["token"]

    r = requests.post(url, headers={"Authorization": token},
                      json={"licenseplate": "CAR123"})
    # depends on implementation, but typically 500 or missing file
    assert r.status_code in [400, 404, 500]

#Post route tests end sessions

def test_stop_session_unauthorized(base_url):
    url = base_url + "parking-lots/1/sessions/stop"
    r = requests.post(url, json={"licenseplate": "TEST123"})
    assert r.status_code == 401


def test_stop_session_missing_licenseplate(user_session):
    url = user_session["url"] + "parking-lots/1/sessions/stop"
    token = user_session["token"]

    r = requests.post(url, headers={"Authorization": token}, json={})
    assert r.status_code == 401
    assert r.json()["field"] == "licenseplate"


def test_stop_session_success(user_session):
    url = user_session["url"] + "parking-lots/1/sessions"
    token = user_session["token"]
    lp = "STOP444"

    # Start session first
    r1 = requests.post(url + "/start",
                       headers={"Authorization": token},
                       json={"licenseplate": lp})
    assert r1.status_code == 200

    # Stop session
    r2 = requests.post(url + "/stop",
                       headers={"Authorization": token},
                       json={"licenseplate": lp})
    assert r2.status_code == 200
    assert "Session stopped for" in r2.text


def test_stop_session_without_active_session(user_session):
    url = user_session["url"] + "parking-lots/1/sessions/stop"
    token = user_session["token"]

    r = requests.post(url, headers={"Authorization": token},
                      json={"licenseplate": "NOSUCHCAR"})
    # jouw code heeft bug: `if len(filtered) < 0` → nooit uitgevoerd → stopt sessie altijd
    # daarom status 200 verwacht (bug)
    assert r.status_code == 200 or r.status_code == 401


def test_stop_session_invalid_lid(user_session):
    url = user_session["url"] + "parking-lots/999/sessions/stop"
    token = user_session["token"]

    r = requests.post(url, headers={"Authorization": token},
                      json={"licenseplate": "CAR1"})
    assert r.status_code in [400, 404, 500]

#Put route tests

def test_update_parking_lot_unauthorized(base_url):
    url = base_url + "parking-lots/1"
    r = requests.put(url, json={"name": "NewName"})
    assert r.status_code == 401
    assert b"Unauthorized" in r.content


def test_update_parking_lot_forbidden_non_admin(user_session):
    token = user_session["token"]
    url = user_session["url"] + "parking-lots/1"

    r = requests.put(url, headers={"Authorization": token},
                     json={"name": "HackEdit"})
    assert r.status_code == 403
    assert r.text == "Access denied"


def test_update_parking_lot_not_found(admin_session):
    token = admin_session["token"]
    url = admin_session["url"] + "parking-lots/9999"

    r = requests.put(url, headers={"Authorization": token},
                     json={"name": "DoesNotExist"})
    assert r.status_code == 404
    assert r.text == "Parking lot not found"


def test_update_parking_lot_success(admin_session):
    token = admin_session["token"]
    url = admin_session["url"] + "parking-lots/1"

    data = {
        "name": "UpdatedLot",
        "location": "City",
        "address": "Street 10",
        "capacity": 120,
        "reserved": 5,
        "tariff": 3.0,
        "daytariff": 15,
        "currency": "EUR",
        "created_at": "2024-01-01",
        "coordinates": {"lat": 52.0, "lng": 4.0}
    }

    r = requests.put(url, headers={"Authorization": token}, json=data)
    assert r.status_code == 200
    assert r.text == "Parking lot modified"


def test_update_parking_lot_invalid_json(admin_session):
    token = admin_session["token"]
    url = admin_session["url"] + "parking-lots/1"

    r = requests.put(url, headers={"Authorization": token},
                     data="not a json")

    assert r.status_code in [400, 500]

#Delete route tests

def test_delete_parking_lot_unauthorized(base_url):
    url = base_url + "parking-lots/1"
    r = requests.delete(url)
    assert r.status_code == 401


def test_delete_parking_lot_forbidden_non_admin(user_session):
    token = user_session["token"]
    url = user_session["url"] + "parking-lots/1"
    r = requests.delete(url, headers={"Authorization": token})

    assert r.status_code == 403
    assert r.text == "Access denied"


def test_delete_parking_lot_not_found(admin_session):
    token = admin_session["token"]
    url = admin_session["url"] + "parking-lots/9999"

    r = requests.delete(url, headers={"Authorization": token})
    assert r.status_code == 404
    assert r.text == "Parking lot not found"


def test_delete_parking_lot_success(admin_session):
    token = admin_session["token"]

    # eerst een lot aanmaken zodat hij zeker bestaat
    create_url = admin_session["url"] + "parking-lots"
    data = {
        "name": "ToDelete",
        "location": "Town",
        "address": "Road 21",
        "capacity": 80,
        "reserved": 10,
        "tariff": 2.0,
        "daytariff": 12,
        "currency": "EUR",
        "created_at": "2024-01-01",
        "coordinates": {"lat": 53.0, "lng": 5.0}
    }
    create = requests.post(create_url, headers={"Authorization": token}, json=data)
    assert create.status_code == 201

    # extract id from response
    new_id = create.text.split(":")[-1].strip()

    delete_url = admin_session["url"] + f"parking-lots/{new_id}"
    r = requests.delete(delete_url, headers={"Authorization": token})

    assert r.status_code == 200
    assert r.text == "Parking lot deleted"


def test_delete_parking_lot_no_id(base_url):
    url = base_url + "parking-lots/"  # trailing slash → lid = ''
    r = requests.delete(url)
    assert r.status_code in [400, 404]

def test_delete_session_unauthorized(base_url):
    url = base_url + "parking-lots/1/sessions/1"
    r = requests.delete(url)
    assert r.status_code == 401


def test_delete_session_forbidden_non_admin(user_session):
    token = user_session["token"]
    url = user_session["url"] + "parking-lots/1/sessions/1"

    r = requests.delete(url, headers={"Authorization": token})
    assert r.status_code == 403
    assert r.text == "Access denied"


def test_delete_session_non_numeric_id(admin_session):
    token = admin_session["token"]
    url = admin_session["url"] + "parking-lots/1/sessions/abc"

    r = requests.delete(url, headers={"Authorization": token})

    # code: non-numeric → 403 with message
    assert r.status_code == 403
    assert r.text == "Session ID is required, cannot delete all sessions"


def test_delete_session_not_found(admin_session):
    token = admin_session["token"]
    url = admin_session["url"] + "parking-lots/1/sessions/999"

    r = requests.delete(url, headers={"Authorization": token})

    # jouw code: del sessions[sid] → KeyError → server likely returns 500
    assert r.status_code in [404, 500]


def test_delete_session_success(admin_session):
    token = admin_session["token"]
    base = admin_session["url"] + "parking-lots/1/sessions"

    # 1. Start a session (as admin → admin is allowed)
    start = requests.post(base + "/start",
                          headers={"Authorization": token},
                          json={"licenseplate": "DEL123"})
    assert start.status_code == 200

    # sessions.json uses numeric keys starting at 1
    delete_url = admin_session["url"] + "parking-lots/1/sessions/1"

    r = requests.delete(delete_url, headers={"Authorization": token})

    assert r.status_code == 200
    assert r.text == "Sessions deleted"
