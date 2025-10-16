import pytest
import requests

PARKING_LOT_ID = "1"


def test_post_vehicle_entry_status_unauthorized(_data):
    url = _data['url'] + 'vehicles/' + PARKING_LOT_ID + '/entry'
    response = requests.post(url, headers={})
    status_code = response.status_code

    assert status_code == 403


def test_post_vehicle_entry_status_authorized(_data):
    url = _data['url'] + 'vehicles/' + PARKING_LOT_ID + '/entry'
    response = requests.post(url, headers={"Authorization": _data['api_key']})
    status_code = response.status_code

    assert status_code == 200


def test_post_vehicle_entry_responsebody(_data):
    url = _data['url'] + 'vehicles/' + PARKING_LOT_ID + '/entry'
    response = requests.post(url, headers={"Authorization": _data['api_key']})

    assert response.status_code == 200
    expected = {
        "message": f"Vehicle entry started for parking lot :{PARKING_LOT_ID}"}
    assert response.json() == expected


def test_post_vehicle_entry_invalid_parkinglot_message(_data):
    invalid_parking_lot_id = "2220000"
    url = _data['url'] + 'vehicles/' + invalid_parking_lot_id + '/entry'
    response = requests.post(url, headers={"Authorization": _data['api_key']})
    assert response.status_code == 400
    expected = {
        "message": f"Parking lot with id: {invalid_parking_lot_id} does not exist"}
    assert response.json() == expected


def test_post_vehicle_entry_nonnumeric_input(_data):
    non_numeric_parking_lot_id = "abc"
    url = _data['url'] + 'vehicles/' + non_numeric_parking_lot_id + '/entry'
    response = requests.post(url, headers={"Authorization": _data['api_key']})
    assert response.status_code == 400
    expected = {
        "message": f"Invalid parking lot ID: {non_numeric_parking_lot_id}. It must be a numeric value."}
    assert response.json() == expected
