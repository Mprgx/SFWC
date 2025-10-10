import pytest
import requests


@pytest.fixture
def _data():
    response = requests.post("http://localhost:8000/login",
                             json={"username": "Mex", "password": "Smpl3Pw!"},)
    token = response.json()["session_token"]

    return {
        "url": "http://localhost:8000/",
        "api_key": token,
    }


@pytest.fixture
def auth_headers(_data):
    return {"Authorization": f"Bearer {_data['api_key']}"}


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
