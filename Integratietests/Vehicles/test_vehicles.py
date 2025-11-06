import requests
import pytest

BASE_PATH = "vehicles/"

# /vehicles tests

# 1️⃣ GET /vehicles – authorized


def test_get_vehicles_authorized(auth_headers, user_session):
    response = requests.get(
        user_session["url"] + BASE_PATH, headers=auth_headers)
    assert response.status_code in (200, 404)


# 2️⃣ GET /vehicles – unauthorized (no token)
def test_get_vehicles_unauthorized(user_session):
    response = requests.get(user_session["url"] + BASE_PATH)
    assert response.status_code == 401

# 3️⃣ GET /vehicles – check required fields in first vehicle


def test_get_vehicles_field_structure(auth_headers, user_session):
    response = requests.get(
        user_session["url"] + BASE_PATH, headers=auth_headers)
    data = response.json()
    if data:
        vehicle = data[0]
        for key in ["licensePlate", "model", "color", "year"]:
            assert key in vehicle

# 4️⃣ GET /vehicles – validate empty list if user has no vehicles


def test_get_vehicles_empty_list_for_new_user(auth_headers, user_session):
    response = requests.get(
        user_session["url"] + BASE_PATH + "?user=newUser", headers=auth_headers)
    assert response.status_code in [200, 404]
    assert isinstance(response.json(), list) or response.json() == []

# 5️⃣ GET /vehicles – check response time


def test_get_vehicles_response_time(auth_headers, user_session):
    response = requests.get(
        user_session["url"] + BASE_PATH, headers=auth_headers)
    assert response.elapsed.total_seconds() < 2

# -------------------------------

# /vehicles/{username} tests

# 1️⃣ GET /vehicles/{username} – authorized


def test_get_vehicles_by_userid_authorized(auth_headers, user_session):
    response = requests.get(
        user_session["url"] + BASE_PATH + "natasjadewit", headers=auth_headers)
    assert response.status_code == 200

# 2️⃣ GET /vehicles/{username} – unauthorized


def test_get_vehicles_by_userid_unauthorized(user_session):
    response = requests.get(user_session["url"] + BASE_PATH + "natasjadewit")
    assert response.status_code == 401

# 3️⃣ GET /vehicles/{username} – check required fields in first vehicle


def test_get_vehicles_field_structure(auth_headers, user_session):
    response = requests.get(
        user_session["url"] + BASE_PATH + "natasjadewit", headers=auth_headers)
    data = response.json()
    if data:
        vehicle = data[0]
        for key in ["licensePlate", "model", "color", "year"]:
            assert key in vehicle

# 4️⃣ GET /vehicles/{username} – validate empty list if user has no vehicles


def test_get_vehicles_empty_list_for_new_user(auth_headers, user_session):
    response = requests.get(
        user_session["url"] + BASE_PATH + "natasjadewit" + "?user=newUser", headers=auth_headers)
    assert response.status_code in [200, 404]
    assert isinstance(response.json(), list) or response.json() == []

# 5️⃣ GET /vehicles/{username} – check response time


def test_get_vehicles_response_time(auth_headers, user_session):
    response = requests.get(
        user_session["url"] + BASE_PATH + "natasjadwit", headers=auth_headers)
    assert response.elapsed.total_seconds() < 2

# -------------------------------

# /vehicles/{vehicleID}/reservations tests

# 1️⃣ GET /vehicles/{vehicleID}/reservations - authorized


def test_get_vehicle_reservation_by_vehicle_id_authorized(auth_headers, user_session):
    response = requests.get(
        user_session["url"] + BASE_PATH + "1/reservations", headers=auth_headers)
    assert response.status_code == 200

# 2️⃣ GET /vehicles/{vehicleID}/reservations - unauthorized


def test_get_vehicle_reservation_by_vehicle_id_unauthorized(auth_headers, user_session):
    response = requests.get(
        user_session["url"] + BASE_PATH + "1/reservations", headers=auth_headers)
    assert response.status_code == 401

# 3️⃣ GET /vehicles/{vehicleID}/reservations - vehicle doesn't exist


def test_vehicle_does_not_exist_reservation(auth_headers, user_session):
    response = requests.get(
        user_session["url"] + BASE_PATH + "999999999/reservations", headers=auth_headers)
    assert response.status_code == 404

# 4️⃣ GET /vehicles/{vehicleID}/reservations - test response time


def test_get_vehicle_reservation_response_time(auth_headers, user_session):
    response = requests.get(
        user_session["url"] + BASE_PATH + "1/reservations", headers=auth_headers)
    assert response.elapsed.total_seconds() < 2

# -------------------------------

# /vehicles/{vehicleID}/history tests

# 1️⃣ GET /vehicles/{vehicleID}/history - authorized


def test_get_vehicle_history_by_vehicle_id_authorized(auth_headers, user_session):
    response = requests.get(
        user_session["url"] + BASE_PATH + "1/history", headers=auth_headers)
    assert response.status_code == 200

# 2️⃣ GET /vehicles/{vehicleID}/history - unauthorized


def test_get_vehicle_history_by_vehicle_id_unauthorized(auth_headers, user_session):
    response = requests.get(
        user_session["url"] + BASE_PATH + "1/history", headers=auth_headers)
    assert response.status_code == 401

# 3️⃣ GET /vehicles/{vehicleID}/history - vehicle doesn't exist


def test_vehicle_does_not_exist_history(auth_headers, user_session):
    response = requests.get(
        user_session["url"] + BASE_PATH + "999999999/history", headers=auth_headers)
    assert response.status_code == 404

# 4️⃣ GET /vehicles/{vehicleID}/history - test response time


def test_get_vehicle_history_response_time(auth_headers, user_session):
    response = requests.get(
        user_session["url"] + BASE_PATH + "1/history", headers=auth_headers)
    assert response.elapsed.total_seconds() < 2
