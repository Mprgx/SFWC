import pytest
import requests


def test_put_reservation_status_unauthorized(user_session):
    url = user_session['url'] + 'reservations'
    response = requests.post(url, headers={})
    status_code = response.status_code
    assert status_code == 403


def test_put_reservation_status_authorized(user_session):
    url = user_session['url'] + 'reservations'
    response = requests.post(
        url, headers={"Authorization": user_session['session_token']})
    status_code = response.status_code
    assert status_code == 200


def test_put_reservation_responsebody(user_session):
    url = user_session['url'] + 'reservations'
    response = requests.post(
        url, headers={"Authorization": user_session['session_token']})
    assert response.status_code == 200
    expected = {
        "message": "Reservation updated successfully"}
    assert response.json() == expected


def test_put_reservation_invalid_id_message(user_session):
    invalid_reservation_id = "9999"
    url = user_session['url'] + 'reservations'
    response = requests.post(
        url, headers={"Authorization": user_session['session_token']})
    assert response.status_code == 400


def test_put_reservation_nonadmin(user_session):
    url = user_session['url'] + 'reservations'
    response = requests.post(
        url, headers={"Authorization": user_session['session_token']})
    assert response.status_code == 400
    expected = {
        "message": "User does not have admin privileges"}
    assert response.json() == expected
