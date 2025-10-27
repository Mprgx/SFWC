import pytest
import requests


def test_parking_lots_authorized(_data):
    url = _data['url'] + 'parking-lots/'
    response = requests.get(url, headers={"Authorization": _data['api_key']})

    assert response.status_code == 200
    assert list(response.json().values())[0] == {
        "id": "1",
        "name": "Bedrijventerrein Almere Parkeergarage",
        "location": "Industrial Zone",
        "address": "Schanssingel 337, 2421 BS Almere",
        "capacity": 335,
        "reserved": 77,
        "tariff": 1.9,
        "daytariff": 11,
        "created_at": "2020-03-25",
        "coordinates": {"lat": 52.3133, "lng": 5.2234}
    }


def test_parking_lots_unauthorized(_data):
    url = _data['url'] + 'parking-lots/'
    response = requests.get(url, headers={})

    assert response.status_code == 401


def test_parking_lots_lid_authorized(_data):
    url = _data['url'] + 'parking-lots/1485'
    response = requests.get(url, headers={"Authorization": _data['api_key']})
    assert response.status_code == 200
    assert response.json() == {
        "id": "1485",
        "name": "Doetinchem Sportcomplex Parkeergarage",
        "location": "Sports Stadium",
        "address": "Havenweg 631, 4858 BR Doetinchem",
        "capacity": 1820,
        "reserved": 387,
        "tariff": 5.6,
        "daytariff": 14,
        "created_at": "2020-12-11",
        "coordinates": {
            "lat": 51.9899,
            "lng": 6.2475
        }
    }


def test_parking_lots_lid_unauthorized(_data):
    url = _data['url'] + 'parking-lots/1485'
    response = requests.get(url, headers={})
    assert response.status_code == 401
