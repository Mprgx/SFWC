import pytest
import requests

LOGIN_CREDENTIALS = {
    "username": "testuser",
    "password": "testpassword1!"
}


def test_get_login_status_successful(user_session):
    url = user_session['url'] + 'login'
    response = requests.post(
        url, json={"username": LOGIN_CREDENTIALS['username'], "password": LOGIN_CREDENTIALS['password']})
    status_code = response.status_code
    assert status_code == 200


def test_get_login_responsebody(user_session):
    url = user_session['url'] + 'login'
    response = requests.post(
        url, json={"username": LOGIN_CREDENTIALS['username'], "password": LOGIN_CREDENTIALS['password']})
    assert response.status_code == 200
    expected = {
        "Authorizationkey": "123123123123123"}
    assert response.json() == expected


def test_get_login_status_bad_request(user_session):
    url = user_session['url'] + 'login'
    response = requests.post(url, json={})
    status_code = response.status_code
    assert status_code == 400


def test_get_login_status_account_doesnt_exist(user_session):
    url = user_session['url'] + 'login'
    response = requests.post(
        url, json={"username": "nonexistentuser", "password": "wrongpassword"})
    status_code = response.status_code
    assert status_code == 401


def test_get_login_responsebody_invalid_login(user_session):
    url = user_session['url'] + 'login'
    response = requests.post(
        url, json={"username": "nonexistentuser", "password": "wrongpassword"})
    assert response.status_code == 401
    expected = {
        "message": "Account with provided credentials does not exist"}
    assert response.json() == expected
