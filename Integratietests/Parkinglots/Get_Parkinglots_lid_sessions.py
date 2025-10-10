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

# Checks if a admin has access to all sessions


def test_get_sessions_as_admin(login_as_admin):
    url = login_as_admin['url'] + '/parking-lots/1/sessions'

    response = requests.get(
        url,
        headers={"Authorization": login_as_admin}
    )
    assert response.status_code == 200
    assert response.headers["Content-Type"] == "application/json"
    assert isinstance(response.json(), list)

# Checks if a user can get all of their sessions


def test_get_own_sessions_as_user(login_as_user):
    url = login_as_admin['url'] + '/parking-lots/1/sessions'

    response = requests.get(
        url,
        headers={"Authorization": login_as_user}
    )
    assert response.status_code == 200
    sessions = response.json()
    for s in sessions:
        assert s['user'] == "User"
