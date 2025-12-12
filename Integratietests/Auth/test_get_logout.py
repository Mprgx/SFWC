import requests

def assert_is_json(response: requests.Response) -> None:
    assert response.headers.get("Content-Type", "").startswith("application/json")

def test_logout_unauthorized_without_token(user_session):
    url = user_session["url"] + "logout"

    response = requests.post(url, verify=False)

    assert response.status_code == 401

def test_logout_unauthorized_invalid_token(user_session):
    url = user_session["url"] + "logout"
    headers = {"Authorization": "Bearer this.is.not.a.valid.token"}

    response = requests.post(url, headers=headers, verify=False)

    assert response.status_code == 401

def test_logout_user_success(user_session):
    url = user_session["url"] + "logout"
    headers = {"Authorization": user_session["session_token"]}

    response = requests.post(url, headers=headers, verify=False)

    assert response.status_code == 200
    assert_is_json(response)

    body = response.json()
    assert body.get("status") == "success"
    assert body.get("message") == "Logged out successfully."


def test_logout_admin_success(admin_session):
    url = admin_session["url"] + "logout"
    headers = {"Authorization": admin_session["session_token"]}

    response = requests.post(url, headers=headers, verify=False)

    assert response.status_code == 200
    assert_is_json(response)
    
    body = response.json()
    assert body.get("status") == "success"
    assert body.get("message") == "Logged out successfully."
