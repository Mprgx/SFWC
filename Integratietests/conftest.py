import pytest
import requests

BASE_URL = "http://localhost:5280/api/"


@pytest.fixture
def user_session():
    resp = requests.post(BASE_URL + "login",
                         json={"username": "Mex", "password": "Hallo123!Hallo"})
    resp.raise_for_status()
    token = "Bearer " + resp.json()["accessToken"]
    return {"url": BASE_URL, "username": "Mex", "accessToken": token}


@pytest.fixture
def admin_session():
    resp = requests.post(BASE_URL + "login",
                         json={"username": "Job", "password": "sendhelp"})
    resp.raise_for_status()
    token = "Bearer " + resp.json()["accessToken"]
    return {"url": BASE_URL, "username": "Job", "accessToken": token}


@pytest.fixture
def auth_headers(user_session):
    return {"Authorization": "Bearer " + user_session["accessToken"]}


@pytest.fixture
def auth_headers_empty_user():
    # token for user with no sessions
    return {"Authorization": "Bearer emptyusertoken"}
