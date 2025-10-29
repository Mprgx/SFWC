import pytest
import requests

VEHICLE_LICENSE = "76-KQQ-7"


def test_post_vehicle_license_status_unauthorized(_data):
    url = _data['url'] + 'vehicles/' + VEHICLE_LICENSE
    response = requests.post(url, headers={})
    status_code = response.status_code

    assert status_code == 403


def test_post_vehicle_license_status_authorized(_data):
    url = _data['url'] + 'vehicles/' + VEHICLE_LICENSE
    response = requests.post(url, headers={"Authorization": _data['api_key']})
    status_code = response.status_code
    assert status_code == 200


def test_post_vehicle_license_responsebody(_data):
    url = _data['url'] + 'vehicles/' + VEHICLE_LICENSE
    response = requests.post(url, headers={"Authorization": _data['api_key']})
    assert response.status_code == 200
    expected = {
        "message": f"Vehicle with licenseplate: {VEHICLE_LICENSE} succesfully updated"}
    assert response.json() == expected


def test_post_vehicle_license_invalid_license_message(_data):
    invalid_license = "INVALID123"
    url = _data['url'] + 'vehicles/' + invalid_license
    response = requests.post(url, headers={"Authorization": _data['api_key']})
    assert response.status_code == 400
    expected = {
        "message": f"Vehicle with licenseplate: {invalid_license} does not exist"}
    assert response.json() == expected


def test_post_vehicle_license_user_doesnt_have_vehicle_to_his_acccount(_data):
    license_not_in_account = "84-WXD-8"
    url = _data['url'] + 'vehicles/' + license_not_in_account
    response = requests.post(url, headers={"Authorization": _data['api_key']})
    assert response.status_code == 400


def test_post_vehicle_license_user_doesnt_have_vehicle_to_his_acccount_message(_data):
    license_not_in_account = "84-WXD-8"
    url = _data['url'] + 'vehicles/' + license_not_in_account
    response = requests.post(url, headers={"Authorization": _data['api_key']})
    assert response.status_code == 400
    expected = {
        "message": f"Vehicle with licenseplate: {license_not_in_account} does not belong to user with id: 8592"}
    assert response.json() == expected
