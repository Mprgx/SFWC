import requests
import pytest

BASE_PATH = "vehicles/"

# /vehicles tests

# 1️⃣ GET /vehicles – authorized
def test_get_vehicles_authorized(auth_headers, _data):
    response = requests.get(_data["url"] + BASE_PATH, headers=auth_headers)
    assert response.status_code == 200
    assert isinstance(response.json(), list)

# 2️⃣ GET /vehicles – unauthorized (no token)
def test_get_vehicles_unauthorized(_data):
    response = requests.get(_data["url"] + BASE_PATH)
    assert response.status_code == 401

# 3️⃣ GET /vehicles – check required fields in first vehicle
def test_get_vehicles_field_structure(auth_headers, _data):
    response = requests.get(_data["url"] + BASE_PATH, headers=auth_headers)
    data = response.json()
    if data:
        vehicle = data[0]
        for key in ["licensePlate", "model", "color", "year"]:
            assert key in vehicle

# 4️⃣ GET /vehicles – validate empty list if user has no vehicles
def test_get_vehicles_empty_list_for_new_user(auth_headers, _data):
    response = requests.get(_data["url"] + BASE_PATH + "?user=newUser", headers=auth_headers)
    assert response.status_code in [200, 404]
    assert isinstance(response.json(), list) or response.json() == []

# 5️⃣ GET /vehicles – check response time
def test_get_vehicles_response_time(auth_headers, _data):
    response = requests.get(_data["url"] + BASE_PATH, headers=auth_headers)
    assert response.elapsed.total_seconds() < 2

# -------------------------------

# /vehicles/{username} tests

# 1️⃣ GET /vehicles/{username} – authorized
def test_get_vehicles_by_userid_authorized(auth_headers, _data):
    response = requests.get(_data["url"] + BASE_PATH + "natasjadewit", headers=auth_headers)
    assert response.status_code == 200

# 2️⃣ GET /vehicles/{username} – unauthorized
def test_get_vehicles_by_userid_unauthorized(_data):
    response = requests.get(_data["url"] + BASE_PATH + "natasjadewit")
    assert response.status_code == 401

# 3️⃣ GET /vehicles/{username} – check required fields in first vehicle
def test_get_vehicles_field_structure(auth_headers, _data):
    response = requests.get(_data["url"] + BASE_PATH + "natasjadewit", headers=auth_headers)
    data = response.json()
    if data:
        vehicle = data[0]
        for key in ["licensePlate", "model", "color", "year"]:
            assert key in vehicle

# 4️⃣ GET /vehicles/{username} – validate empty list if user has no vehicles
def test_get_vehicles_empty_list_for_new_user(auth_headers, _data):
    response = requests.get(_data["url"] + BASE_PATH + "natasjadewit" + "?user=newUser", headers=auth_headers)
    assert response.status_code in [200, 404]
    assert isinstance(response.json(), list) or response.json() == []

# 5️⃣ GET /vehicles/{username} – check response time
def test_get_vehicles_response_time(auth_headers, _data):
    response = requests.get(_data["url"] + BASE_PATH + "natasjadwit", headers=auth_headers)
    assert response.elapsed.total_seconds() < 2