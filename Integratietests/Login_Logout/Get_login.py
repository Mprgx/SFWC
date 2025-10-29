import pytest
import requests

LOGIN_CREDENTIALS = {
    "username": "testuser",
    "password": "testpassword"
}


def test_get_login_status_successful(_data):
    url = _data['url'] + 'login'
    response = requests.get(
        url, json={LOGIN_CREDENTIALS['username'], LOGIN_CREDENTIALS['password']})
    status_code = response.status_code
    assert status_code == 200


def test_get_login_responsebody(_data):
    url = _data['url'] + 'login'
    response = requests.get(
        url, json={LOGIN_CREDENTIALS['username'], LOGIN_CREDENTIALS['password']})
    assert response.status_code == 200
    expected = {
        "Authorizationkey": "123123123123123"}
    assert response.json() == expected


def test_get_login_status_bad_request(_data):
    url = _data['url'] + 'login'
    response = requests.get(url, json={})
    status_code = response.status_code
    assert status_code == 400


def test_get_login_status_account_doesnt_exist(_data):
    url = _data['url'] + 'login'
    response = requests.get(
        url, json={"username": "nonexistentuser", "password": "wrongpassword"})
    status_code = response.status_code
    assert status_code == 400


def test_get_login_responsebody_invalid_login(_data):
    url = _data['url'] + 'login'
    response = requests.get(
        url, json={"username": "nonexistentuser", "password": "wrongpassword"})
    assert response.status_code == 400
    expected = {
        "message": "Account with provided credentials does not exist"}
    assert response.json() == expected
