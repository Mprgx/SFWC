import pytest
import requests

RESERVATION_ID = "1"


def test_get_reservation_status_unauthorized(_data):
    url = _data['url'] + 'reservations/' + RESERVATION_ID
    response = requests.get(url, headers={})
    status_code = response.status_code

    assert status_code == 403


def test_get_reservation_status_authorized(_data):
    url = _data['url'] + 'reservations/' + RESERVATION_ID
    response = requests.get(url, headers={"Authorization": _data['api_key']})
    status_code = response.status_code
    assert status_code == 200


def test_get_reservation_responsebody(_data):
    url = _data['url'] + 'reservations/' + RESERVATION_ID
    response = requests.get(url, headers={"Authorization": _data['api_key']})
    assert response.status_code == 200
    expected = {f"Reservation details for reservation ID: {RESERVATION_ID}, user ID: 1, licenseplate: AB-123-CD, startdate: 11.12.2025, enddate: 12.12.2025, parkinglot: 1"}
    assert response.json() == expected


def test_get_reservation_invalid_id(_data):
    invalid_reservation_id = "9999"
    url = _data['url'] + 'reservations/' + invalid_reservation_id
    response = requests.get(url, headers={"Authorization": _data['api_key']})
    assert response.status_code == 400


def test_get_reservation_invalid_id_message(_data):
    invalid_reservation_id = "9999"
    url = _data['url'] + 'reservations/' + invalid_reservation_id
    response = requests.get(url, headers={"Authorization": _data['api_key']})
    assert response.status_code == 400
    expected = {
        "message": f"Reservation with ID: {invalid_reservation_id} does not exist"}
    assert response.json() == expected
