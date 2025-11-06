import pytest
import requests

RESERVATION = {
    "licenseplate": "AB-123-CD",
    "startdate": "11.12.2025",
    "enddate": "12.12.2025",
    "parkinglot": "1",
}

RESERVATION_RESERVED = {
    "licenseplate": "AB-123-CD",
    "startdate": "11.12.2025",
    "enddate": "12.12.2025",
    "parkinglot": "4",
}


def test_post_reservation_status_unauthorized(user_session):
    url = user_session['url'] + 'reservations'
    response = requests.post(url, headers={})
    status_code = response.status_code

    assert status_code == 403


def test_post_reservation_status_authorized(user_session):
    url = user_session['url'] + 'reservations'
    response = requests.post(url, json=RESERVATION, headers={
                             "Authorization": user_session['session_token']})
    status_code = response.status_code
    assert status_code == 201


def test_post_reservation_message(user_session):
    url = user_session['url'] + 'reservations'
    response = requests.post(url, json=RESERVATION, headers={
                             "Authorization": user_session['session_token']})
    assert response.status_code == 201
    expected = {
        "message": f"Reservation for vehicle with licenseplate: {RESERVATION['licenseplate']} from {RESERVATION['startdate']} to {RESERVATION['enddate']} at parking lot {RESERVATION['parkinglot']} has been created successfully."}
    assert response.json() == expected


def test_post_reservation_invaliduser_session(user_session):
    url = user_session['url'] + 'reservations'
    invaliduser_session = RESERVATION.copy()
    invaliduser_session["startdate"] = "invalid-date"
    response = requests.post(url, json=invaliduser_session, headers={
                             "Authorization": user_session['session_token']})
    assert response.status_code == 400
    expected = {
        "message": "Invalid date format for startdate. Expected format: DD.MM.YYYY"}
    assert response.json() == expected


def test_post_reservation_missing_field(user_session):
    url = user_session['url'] + 'reservations'
    incompleteuser_session = RESERVATION.copy()
    del incompleteuser_session["licenseplate"]
    response = requests.post(url, json=incompleteuser_session, headers={
                             "Authorization": user_session['session_token']})
    assert response.status_code == 400
    expected = {
        "message": "Missing required field: licenseplate"}
    assert response.json() == expected


def test_post_reservation_parkinglot_already_reserved_message(user_session):
    url = user_session['url'] + 'reservations'
    response = requests.post(url, json=RESERVATION_RESERVED, headers={
                             "Authorization": user_session['session_token']})
    assert response.status_code == 400
    expected = {
        "message": f"Parkinglot {RESERVATION_RESERVED['parkinglot']}is already booked for date: {RESERVATION_RESERVED['startdate']}"}
    assert response.json() == expected
