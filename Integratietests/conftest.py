import pytest
import requests


@pytest.fixture
def _data():
    response = requests.post("http://localhost:8000/login",
                             json={"username": "Mex", "password": "Smpl3Pw!"},)

    if response.status_code != 200:
        pytest.fail(
            f"Login failed with status {response.status_code}: {response.text}")

    try:
        token = response.json()["session_token"]
    except (ValueError, KeyError):
        pytest.fail(
            f"Failed to get session token from login response: {response.text}")

    return {
        "url": "http://localhost:8000/",
        "api_key": token,
    }


@pytest.fixture
def auth_headers(_data):
    return {"Authorization": f"Bearer {_data['api_key']}"}
