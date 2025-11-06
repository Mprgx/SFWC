import pytest
import requests

VEHICLE_PAYLOAD = {
    "user_id": "8592",
    "license_plate": "84-WXD-8",
    "make": "Toyota",
    "model": "Camry",
    "color": "Navy",
    "year": 2024,
}


def test_post_vehicle_status_unauthorized(user_session):
    url = user_session['url'] + 'vehicles'
    response = requests.post(url, json=VEHICLE_PAYLOAD, headers={})
    status_code = response.status_code

    assert status_code == 401


def test_post_vehicle_status_bad_request(user_session):
    url = user_session['url'] + 'vehicles'
    response = requests.post(url, json={}, headers={
                             "Authorization": user_session['session_token']})
    status_code = response.status_code

    assert status_code == 400


def test_post_vehicles_ok_message(user_session):
    url = user_session['url'] + 'vehicles'
    response = requests.post(url, json=VEHICLE_PAYLOAD, headers={
        "Authorization": user_session['session_token']})

    assert response.status_code == 200
    expected = {
        f"Vehicle with licenseplate: {VEHICLE_PAYLOAD['license_plate']} succesfully added"}
    assert response.json() == expected

    del_resp = requests.delete(
        url + f"/{VEHICLE_PAYLOAD['license_plate']}", headers={"Authorization": user_session['session_token']})
    assert del_resp.status_code == 200


# def test_vehicle_json_adjusted_ok(user_session):
#     url = user_session['url'] + 'vehicles'
#     response = requests.post(url, json=VEHICLE_PAYLOAD, headers={
#                              "Authorization": user_session['session_token']})
