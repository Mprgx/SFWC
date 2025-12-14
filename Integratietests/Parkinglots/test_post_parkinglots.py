import requests
import pytest


def test_create_parking_lot_unauthorized(login_as_user):
    url = login_as_user["url"] + "parking-lots"
    r = requests.post(url, json={}, verify=False)
    assert r.status_code == 401


def test_create_parking_lot_user_forbidden(login_as_user):
    url = login_as_user["url"] + "parking-lots"
    payload = {
        "name": "TestLot",
        "location": "City",
        "address": "Address 1",
        "capacity": 50,
        "tariff": 1.5,
        "dayTariff": 10,
        "coordinates": {"latitude": 1, "longitude": 1},
    }

    headers = {"Authorization": login_as_user["session_token"]}
    r = requests.post(url, headers=headers, json=payload, verify=False)
    assert r.status_code == 403


def test_create_parking_lot_success(login_as_admin):
    url = login_as_admin["url"] + "parking-lots"
    payload = {
        "name": "LotA",
        "location": "Center",
        "address": "Main St",
        "capacity": 100,
        "tariff": 2.0,
        "dayTariff": 12,
        "coordinates": {"latitude": 52.0, "longitude": 5.1},
    }

    headers = {"Authorization": login_as_admin["session_token"]}
    r = requests.post(url, headers=headers, json=payload, verify=False)

    assert r.status_code == 200
    assert r.json()["name"] == "LotA"


def test_create_parking_lot_invalid_payload(login_as_admin):
    url = login_as_admin["url"] + "parking-lots"
    headers = {"Authorization": login_as_admin["session_token"]}

    r = requests.post(url, headers=headers, json={"name": ""}, verify=False)
    assert r.status_code in (400, 422)


def test_create_parking_lot_response_is_json(login_as_admin):
    url = login_as_admin["url"] + "parking-lots"
    headers = {"Authorization": login_as_admin["session_token"]}

    payload = {
        "name": "JsonLot",
        "location": "City",
        "address": "Somewhere",
        "capacity": 20,
        "tariff": 1,
        "dayTariff": 8,
        "coordinates": {"latitude": 10, "longitude": 20}
    }

    r = requests.post(url, headers=headers, json=payload, verify=False)
    assert r.headers.get("Content-Type", "").startswith("application/json")
