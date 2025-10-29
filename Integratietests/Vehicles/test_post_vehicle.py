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


def test_post_vehicle_status_unauthorized(_data):
    url = _data['url'] + 'vehicles'
    response = requests.post(url, json=VEHICLE_PAYLOAD, headers={})
    status_code = response.status_code

    assert status_code == 403


def test_post_vehicle_status_bad_request(_data):
    url = _data['url'] + 'vehicles'
    response = requests.post(url, json={}, headers={
                             "Authorization": _data['api_key']})
    status_code = response.status_code

    assert status_code == 400


def test_get_vehicles_ok_responsebody(_data):
    url = _data['url'] + 'vehicles'
    response = requests.post(url, json=VEHICLE_PAYLOAD, headers={
        "Authorization": _data['api_key']})

    assert response.status_code == 200
    expected = {
        f"Vehicle with licenseplate: {VEHICLE_PAYLOAD['license_plate']} succesfully added"}
    assert response.json() == expected

    del_resp = requests.delete(
        url + f"/{VEHICLE_PAYLOAD['license_plate']}", headers={"Authorization": _data['api_key']})
    assert del_resp.status_code == 200


# def test_vehicle_json_adjusted_ok(_data):
#     url = _data['url'] + 'vehicles'
#     response = requests.post(url, json=VEHICLE_PAYLOAD, headers={
#                              "Authorization": _data['api_key']})
