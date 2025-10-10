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
