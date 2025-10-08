import pytest
import requests


def test_get_profile_authorized(_data):
    url = _data['url'] + 'profile'
    response = requests.get(url, headers={"Authorization": _data['api_key']})
    status_code = response.status_code

    assert status_code == 200


def test_get_profile_unauthorized(_data):
    url = _data['url'] + 'profile'
    response = requests.get(url, headers={})
    status_code = response.status_code

    assert status_code == 401
