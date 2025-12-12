import requests
import pytest

VERIFY = False  # Zelf-ondertekend cert

# GET /vehicles


def test_get_my_vehicles_success(login_as_user):
    url = login_as_user["url"] + "vehicles"
    headers = {"Authorization": login_as_user["session_token"]}
    r = requests.get(url, headers=headers, verify=VERIFY)
    assert r.status_code == 200
    assert isinstance(r.json(), list)


def test_get_my_vehicles_no_token(login_as_user):
    url = login_as_user["url"] + "vehicles"
    r = requests.get(url, verify=VERIFY)
    assert r.status_code == 401


def test_get_my_vehicles_invalid_token(auth_headers_empty_user):
    url = auth_headers_empty_user.get(
        "url", "https://localhost:7197/") + "vehicles"
    r = requests.get(url, headers=auth_headers_empty_user, verify=VERIFY)
    assert r.status_code == 401

# -------------------------
# GET /vehicle/{username} (Admin only)
# -------------------------


def test_admin_get_vehicle_by_username_success(login_as_admin, login_as_user):
    payload = {
        "licensePlate": "USR-Q1",
        "make": "VW",
        "model": "Polo",
        "color": "Blue",
        "year": 2010,
    }
    headers_user = {"Authorization": login_as_user["session_token"]}
    requests.post(login_as_user["url"] + "vehicle",
                  json=payload, headers=headers_user, verify=VERIFY)

    url = login_as_admin["url"] + f"vehicle/{login_as_user['username']}"
    headers_admin = {"Authorization": login_as_admin["session_token"]}
    r = requests.get(url, headers=headers_admin, verify=VERIFY)
    assert r.status_code == 200
    assert isinstance(r.json(), list)


def test_admin_get_vehicle_by_username_no_content(login_as_admin):
    url = login_as_admin["url"] + "vehicle/no_such_user_999"
    headers = {"Authorization": login_as_admin["session_token"]}
    r = requests.get(url, headers=headers, verify=VERIFY)
    assert r.status_code == 204


def test_admin_get_vehicle_by_username_forbidden(login_as_user, login_as_admin):
    # normale user mag niet admin endpoint
    url = login_as_user["url"] + "vehicle/" + login_as_user["username"]
    headers = {"Authorization": login_as_user["session_token"]}
    r = requests.get(url, headers=headers, verify=VERIFY)
    assert r.status_code == 403


def test_admin_get_vehicle_by_username_no_token(login_as_admin):
    url = login_as_admin["url"] + "vehicle/testuser"
    r = requests.get(url, verify=VERIFY)
    assert r.status_code == 401


def test_admin_get_vehicle_by_username_invalid_token(auth_headers_empty_user):
    url = auth_headers_empty_user.get(
        "url", "https://localhost:7197/") + "vehicle/someone"
    r = requests.get(url, headers=auth_headers_empty_user, verify=VERIFY)
    assert r.status_code in (401, 403)
