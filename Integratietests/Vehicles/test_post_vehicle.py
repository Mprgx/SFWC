import uuid
import requests
import pytest

VERIFY = False  # Zelf-ondertekend cert

# POST /vehicle


def test_create_vehicle_success(login_as_user):
    url = login_as_user["url"] + "vehicle"

    license_plate = f"TST-{uuid.uuid4().hex[:6]}"

    payload = {
        "licensePlate": license_plate,
        "make": "Tesla",
        "model": "Model S",
        "color": "Black",
        "year": 2022,
    }

    headers = {
        "Authorization": f"Bearer {login_as_user['accessToken']}"
    }

    r = requests.post(url, json=payload, headers=headers, verify=VERIFY)

    assert r.status_code == 200
    assert r.json()["licensePlate"] == license_plate.upper()


def test_create_vehicle_duplicate_plate(login_as_user):
    url = login_as_user["url"] + "vehicle"
    payload = {
        "licensePlate": "DUP-001",
        "make": "Ford",
        "model": "Focus",
        "color": "Blue",
        "year": 2018,
    }
    headers = {"Authorization": login_as_user["session_token"]}

    requests.post(url, json=payload, headers=headers, verify=VERIFY)
    r2 = requests.post(url, json=payload, headers=headers, verify=VERIFY)
    assert r2.status_code == 409


def test_create_vehicle_no_token(login_as_user):
    url = login_as_user["url"] + "vehicle"
    r = requests.post(url, json={}, verify=VERIFY)
    assert r.status_code == 401


def test_create_vehicle_invalid_token(auth_headers_empty_user):
    url = auth_headers_empty_user.get(
        "url", "https://localhost:7197/") + "vehicle"
    r = requests.post(url, json={"licensePlate": "X"},
                      headers=auth_headers_empty_user, verify=VERIFY)
    assert r.status_code == 401
