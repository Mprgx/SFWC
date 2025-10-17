import pytest
import requests

BASE_URL = "http://localhost:8000/"

@pytest.fixture
def user_session():
    resp = requests.post(BASE_URL + "login", json={"username": "Mex", "password": "Smpl3Pw!"})
    resp.raise_for_status()
    token = resp.json()["session_token"]
    return {"url": BASE_URL, "username": "Mex", "session_token": token}

@pytest.fixture
def admin_session():
    resp = requests.post(BASE_URL + "login", json={"username": "Job", "password": "sendhelp"})
    resp.raise_for_status()
    token = resp.json()["session_token"]
    return {"url": BASE_URL, "username": "Job", "session_token": token}

@pytest.fixture
def auth_headers(user_session):
    return {"Authorization": user_session["session_token"]}