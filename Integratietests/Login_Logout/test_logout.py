import pytest
import requests


def test_logout_unauthorized(_data):
    url = _data['url'] + 'logout'
    response = requests.get(url, headers={})

    assert response.status_code == 400


def test_logout_authorized(_data):
    url = _data['url'] + 'logout'
    response = requests.get(url, headers={"Authorization": _data['api_key']})

    assert response.status_code == 200
    assert response.text == "User logged out"
