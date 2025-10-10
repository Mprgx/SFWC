import pytest
import requests


@pytest.fixture
def login_as_admin():

    url = "http://localhost:8000"
    login_url = url + 'login'

    login_response = requests.post(
        login_url, json={"username": "Job", "password": "sendhelp"})

    assert login_response.status_code == 200
    session_token = login_response.json().get('session_token')
    assert session_token is not None

    return {
        'url': url,
        'session_token': session_token
    }


@pytest.fixture
def login_as_user():

    url = "http://localhost:8000"
    login_url = url + 'login'

    login_response = requests.post(
        login_url, json={"username": "Job", "password": "sendhelp"})

    assert login_response.status_code == 200
    session_token = login_response.json().get('session_token')
    assert session_token is not None

    return {
        'url': url,
        'session_token': session_token
    }

# Checks if a parking lot isn't found


def test_parking_lot_not_found(login_as_admin):
    url = login_as_admin['url'] + '/parking-lots/0'

    response = requests.get(
        url,
        headers={"Authorization": login_as_admin['session_token']})

    assert response.status_code == 404
    assert response.text == "Parking lot not found"

# Checks if a parking lot is found


def test_parking_lot_found(login_as_admin):
    url = login_as_admin['url'] + '/parking-lots/1'

    response = requests.get(
        url,
        headers={"Authorization": login_as_admin['session_token']})

    assert response.status_code == 200
    assert response.headers["Content-Type"] == "application/json"

# Checks if you can access a session as a admin without a token


def test_sessions_unauthorized(login_as_admin):
    url = login_as_admin['url'] + '/parking-lots/1/sessions'

    response = requests.get(url)
    assert response.status_code == 403
    assert response.text == "Unauthorized: Invalid or missing session token"

# Checks if you can access a session as a user without a token


def test_sessions_unauthorized(login_as_user):
    url = login_as_user['url'] + '/parking-lots/1/sessions'

    response = requests.get(url)
    assert response.status_code == 403
    assert response.text == "Unauthorized: Invalid or missing session token"
