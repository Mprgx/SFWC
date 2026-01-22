import pytest
import requests

RESERVATION_ID = "1"


def test_get_reservation_status_unauthorized(user_session):
    url = user_session['url'] + 'reservations/by-id/' + RESERVATION_ID
    response = requests.get(url, headers={})
    status_code = response.status_code

    assert status_code == 401


def test_get_reservation_status_authorized(user_session):
    url = user_session['url'] + 'reservations/by-id/' + RESERVATION_ID
    response = requests.get(
        url, headers={"Authorization": user_session['session_token']})
    status_code = response.status_code
    assert status_code == 200


def test_get_reservation_correct_message(user_session):
    url = user_session['url'] + 'reservations/by-id/' + RESERVATION_ID
    response = requests.get(
        url, headers={"Authorization": user_session['session_token']})
    assert response.status_code == 200
    # Verify the response has the expected structure and fields
    data = response.json()
    assert data['id'] == 1
    assert 'userId' in data or 'user_id' in data
    assert 'parkingLotId' in data or 'parking_lot_id' in data
    assert 'vehicleId' in data or 'vehicle_id' in data
    assert 'startTime' in data or 'start_time' in data
    assert 'endTime' in data or 'end_time' in data


def test_get_reservation_invalid_id(user_session):
    invalid_reservation_id = "9999"
    url = user_session['url'] + 'reservations/by-id/' + invalid_reservation_id
    response = requests.get(
        url, headers={"Authorization": user_session['session_token']})
    assert response.status_code == 404


def test_get_reservation_invalid_id_message(user_session):
    invalid_reservation_id = "9999"
    url = user_session['url'] + 'reservations/by-id/' + invalid_reservation_id
    response = requests.get(
        url, headers={"Authorization": user_session['session_token']})
    assert response.status_code == 404
    response_data = response.json()
    assert 'message' in response_data
    assert 'does not exist' in response_data['message'].lower(
    ) or invalid_reservation_id in response_data['message']


def test_get_reservation_verify_ownership(user_session):
    # Test that user can access their own reservation
    url = user_session['url'] + 'reservations/by-id/' + RESERVATION_ID
    response = requests.get(
        url, headers={"Authorization": user_session['session_token']})
    # User should be able to access their own reservation
    assert response.status_code == 200
    data = response.json()
    # Verify the reservation data is returned
    assert data['id'] == int(RESERVATION_ID)
