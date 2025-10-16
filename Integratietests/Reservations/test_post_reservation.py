import pytest
import requests

RESERVATION_DATA = {
    "UserID": "1",
    "licenseplate": "AB-123-CD",
    "startdate": "11.12.2025",
    "enddate": "12.12.2025",
    "parkinglot": "1",
}


def test_post_reservation_status_unauthorized(_data):
    url = _data['url'] + 'reservations/'
    response = requests.post(url, headers={})
    status_code = response.status_code

    assert status_code == 403


def test_post_reservation_status_authorized(_data):
    url = _data['url'] + 'reservations/'
    response = requests.post(url, json=RESERVATION_DATA, headers={
                             "Authorization": _data['api_key']})
    status_code = response.status_code
    assert status_code == 201


def test_post_reservation_responsebody(_data):
    url = _data['url'] + 'reservations/'
    response = requests.post(url, json=RESERVATION_DATA, headers={
                             "Authorization": _data['api_key']})
    assert response.status_code == 201
    expected = {
        "message": f"Reservation for vehicle with licenseplate: {RESERVATION_DATA['licenseplate']} from {RESERVATION_DATA['startdate']} to {RESERVATION_DATA['enddate']} at parking lot {RESERVATION_DATA['parkinglot']} has been created successfully."}
    assert response.json() == expected


def test_post_reservation_invalid_data(_data):
    url = _data['url'] + 'reservations/'
    invalid_data = RESERVATION_DATA.copy()
    invalid_data["startdate"] = "invalid-date"
    response = requests.post(url, json=invalid_data, headers={
                             "Authorization": _data['api_key']})
    assert response.status_code == 400
    expected = {
        "message": "Invalid date format for startdate. Expected format: DD.MM.YYYY"}
    assert response.json() == expected


def test_post_reservation_missing_field(_data):
    url = _data['url'] + 'reservations/'
    incomplete_data = RESERVATION_DATA.copy()
    del incomplete_data["licenseplate"]
    response = requests.post(url, json=incomplete_data, headers={
                             "Authorization": _data['api_key']})
    assert response.status_code == 400
    expected = {
        "message": "Missing required field: licenseplate"}
    assert response.json() == expected
