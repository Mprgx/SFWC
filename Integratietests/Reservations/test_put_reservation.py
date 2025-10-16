import pytest
import requests


def test_post_reservation_status_unauthorized(_data):
    url = _data['url'] + 'reservations'
    response = requests.post(url, headers={})
    status_code = response.status_code
    assert status_code == 403


def test_post_reservation_status_authorized(_data):
    url = _data['url'] + 'reservations'
    response = requests.post(url, headers={"Authorization": _data['api_key']})
    status_code = response.status_code
    assert status_code == 200


def test_post_reservation_responsebody(_data):
    url = _data['url'] + 'reservations'
    response = requests.post(url, headers={"Authorization": _data['api_key']})
    assert response.status_code == 200
    expected = {
        "message": "Reservation updated successfully"}
    assert response.json() == expected


def test_post_reservation_invalid_id_message(_data):
    invalid_reservation_id = "9999"
    url = _data['url'] + 'reservations'
    response = requests.post(url, headers={"Authorization": _data['api_key']})
    assert response.status_code == 400


def test_post_reservation_nonadmin(_data):
    url = _data['url'] + 'reservations'
    response = requests.post(url, headers={"Authorization": _data['api_key']})
    assert response.status_code == 400
    expected = {
        "message": "User does not have admin privileges"}
    assert response.json() == expected
