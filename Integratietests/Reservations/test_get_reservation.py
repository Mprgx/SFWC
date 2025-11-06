import pytest
import requests

RESERVATION_ID = "1"


def test_get_reservation_status_unauthorized(user_session):
    url = user_session['url'] + 'reservations/' + RESERVATION_ID
    response = requests.get(url, headers={})
    status_code = response.status_code

    assert status_code == 401


def test_get_reservation_status_authorized(user_session):
    url = user_session['url'] + 'reservations/' + RESERVATION_ID
    response = requests.get(
        url, headers={"Authorization": user_session['session_token']})
    status_code = response.status_code
    assert status_code == 200


def test_get_reservation_correct_message(user_session):
    url = user_session['url'] + 'reservations/' + RESERVATION_ID
    response = requests.get(
        url, headers={"Authorization": user_session['session_token']})
    assert response.status_code == 200
    expected = {
        "id": 1,
        "user_id": 281,
        "parking_lot_id": 217,
        "vehicle_id": 471,
        "start_time": "2025-12-03T11:00:00Z",
        "end_time": "2025-12-03T14:00:00Z",
        "status": "confirmed",
        "created_at": "2025-12-01T11:00:00Z",
        "cost": 7.5
    }
    assert response.json() == expected


def test_get_reservation_invalid_id(user_session):
    invalid_reservation_id = "9999"
    url = user_session['url'] + 'reservations/' + invalid_reservation_id
    response = requests.get(
        url, headers={"Authorization": user_session['session_token']})
    assert response.status_code == 403


def test_get_reservation_invalid_id_message(user_session):
    invalid_reservation_id = "9999"
    url = user_session['url'] + 'reservations/' + invalid_reservation_id
    response = requests.get(
        url, headers={"Authorization": user_session['session_token']})
    assert response.status_code == 403
    expected = {
        "message": f"Reservation with ID: {invalid_reservation_id} does not exist"}
    assert response.json() == expected


def test_get_reservation_not_users_reservation_message(user_session):

    url = user_session['url'] + 'reservations/' + RESERVATION_ID
    response = requests.get(
        url, headers={"Authorization": user_session['session_token']})
    assert response.status_code == 403
    expected = {
        "message": f"{RESERVATION_ID} doesnt belong to the logged in user."}
    assert response.json == expected
