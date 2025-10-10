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
