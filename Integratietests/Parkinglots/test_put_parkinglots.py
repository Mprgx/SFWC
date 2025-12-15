import requests
import re


def test_update_parking_lot_unauthorized(login_as_user):
    url = login_as_user["url"] + "parking-lots/1"

    r = requests.put(url, json={}, verify=False)
    assert r.status_code == 401


def test_update_parking_lot_forbidden(login_as_user):
    # normal user (not admin)
    url = login_as_user["url"] + "parking-lots/1"
    headers = {"Authorization": login_as_user["session_token"]}

    r = requests.put(url, json={}, headers=headers, verify=False)
    assert r.status_code == 403


def test_update_parking_lot_not_found(login_as_admin):
    url = login_as_admin["url"] + "parking-lots/fake"
    headers = {"Authorization": login_as_admin["session_token"]}

    payload = {"name": "Updated"}
    r = requests.put(url, headers=headers,
                     json=payload, verify=False)
    assert r.status_code == 404


def test_update_parking_lot_success(login_as_admin):
    url = login_as_admin["url"] + "parking-lots"
    headers = {"Authorization": login_as_admin["session_token"]}

    payload = {
        "name": "UpdLot",
        "location": "L",
        "address": "A",
        "capacity": 10,
        "tariff": 1,
        "dayTariff": 5,
        "coordinates": {"latitude": 0, "longitude": 0},
    }

    # create lot
    created = requests.post(url, headers=headers,
                            json=payload, verify=False)

    assert created.status_code == 201
    
    lot_id = created.json()["id"]

    # update
    new_url = login_as_admin["url"] + f"parking-lots/{lot_id}"
    new_headers = {"Authorization": login_as_admin["session_token"]}
    r = requests.put(new_url, headers=new_headers, json={"name": "UpdatedName"}, verify=False)

    assert r.status_code == 200


def test_update_parking_lot_wrong_token(login_as_admin):
    url = login_as_admin["url"] + "parking-lots/1"

    r = requests.put(url, headers={"Authorization": "Fake"}, json={"name": "X"}, verify=False)
    assert r.status_code == 401
