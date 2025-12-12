import uuid
import requests

TEST_USER_PREFIX = "test_"

def _unique_username():
    return TEST_USER_PREFIX + uuid.uuid4().hex[:10]

def assert_is_json(response: requests.Response) -> None:
    assert response.headers.get("Content-Type", "").startswith("application/json")


def assert_is_problem_json(response: requests.Response) -> None:
    assert response.headers.get("Content-Type", "").startswith("application/problem+json")

def test_register_creates_user_returns_201_and_json_contract(user_session):
    base_url = user_session["url"]          
    url = base_url + "register"
    username = _unique_username()
    password = "Smpl3Pw!"

    payload = {
        "username": username,
        "password": password,
        "name": "Test Name",
    }

    response = requests.post(url, json=payload, verify=False)

    assert response.status_code == 201
    assert_is_json(response)

    body = response.json()
    assert body.get("status") == "success"
    assert "user" in body
    assert isinstance(body["user"], dict)

    login_url = base_url + "login"
    login = requests.post(
        login_url,
        json={"username": username, "password": password},
        verify=False,
    )

    assert login.status_code == 200
    assert_is_json(login)

    login_body = login.json()
    assert login_body.get("status") == "success"
    assert "accessToken" in login_body
    assert "refreshToken" in login_body

def test_register_missing_fields_returns_400(user_session):
    url = user_session["url"] + "register"

    response = requests.post(url, json={}, verify=False)

    assert response.status_code == 400
    assert_is_problem_json(response)

def test_register_duplicate_username_returns_409(user_session):
    url = user_session["url"] + "register"
    username = _unique_username()
    base_payload = {
        "username": username,
        "password": "abc12345",
        "name": "Duplicate",
    }

    response1 = requests.post(url, json=base_payload, verify=False)
    assert response1.status_code == 201
    assert_is_json(response1)

    response2 = requests.post(url, json=base_payload, verify=False)
    assert response2.status_code == 409
    assert_is_json(response2)

    body = response2.json()
    assert body.get("status") == "error"
    assert body.get("message") == "Username or email already exists."
