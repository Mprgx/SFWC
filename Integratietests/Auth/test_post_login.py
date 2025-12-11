import pytest
import requests

LOGIN_CREDENTIALS = {
    "username": "usersharad",
    "password": "Sharad2002!",
}

def assert_is_json(response: requests.Response) -> None:
    assert response.headers.get("Content-Type", "").startswith("application/json")


def assert_is_problem_json(response: requests.Response) -> None:
    assert response.headers.get("Content-Type", "").startswith("application/problem+json")

def test_get_login_status_successful(user_session):
    url = user_session['url'] + 'login'

    response = requests.post(
        url,
        json=LOGIN_CREDENTIALS,
        verify=False,
    )

    assert response.status_code == 200
    assert_is_json(response)


def test_get_login_responsebody(user_session):
    url = user_session["url"] + "login"

    response = requests.post(
        url,
        json=LOGIN_CREDENTIALS,
        verify=False,
    )

    assert response.status_code == 200
    assert_is_json(response)

    body = response.json()
    assert body.get("status") == "success"
    assert "accessToken" in body
    assert isinstance(body["accessToken"], str)
    assert "refreshToken" in body
    assert isinstance(body["refreshToken"], str)


def test_get_login_status_bad_request(user_session):
    url = user_session["url"] + "login"

    response = requests.post(
        url,
        json={},
        verify=False,
    )

    assert response.status_code == 400
    assert_is_problem_json(response)


def test_get_login_status_account_doesnt_exist(user_session):
    url = user_session["url"] + "login"

    response = requests.post(
        url,
        json={"username": "nonexistentuser", "password": "wrongpassword"},
        verify=False,
    )

    assert response.status_code == 401
    assert_is_json(response)


def test_get_login_responsebody_invalid_login(user_session):
    url = user_session["url"] + "login"

    response = requests.post(
        url,
        json={"username": "nonexistentuser", "password": "wrongpassword"},
        verify=False,
    )

    assert response.status_code == 401
    assert_is_json(response)

    body = response.json()
    assert body.get("status") == "error"
    assert body.get("message") == "Invalid username or password."
