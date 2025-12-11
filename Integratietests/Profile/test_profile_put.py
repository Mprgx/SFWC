import uuid
import requests

def assert_is_json(response: requests.Response) -> None:
    assert response.headers.get("Content-Type", "").startswith("application/json")

def assert_is_problem_json(response: requests.Response) -> None:
    assert response.headers.get("Content-Type", "").startswith("application/problem+json")

def test_profile_update_requires_authorization(user_session):
    url = user_session["url"] + "profile"
    payload = {"name": "Hacker Name"}

    response = requests.put(url, json=payload, verify=False)
    assert response.status_code == 401


def test_profile_update_succeeds_with_valid_data(user_session):
    url = user_session["url"] + "profile"
    headers = {"Authorization": user_session["session_token"]}

    before = requests.get(url, headers=headers, verify=False)
    assert before.status_code == 200
    assert_is_json(before)
    before_data = before.json()
    original_name = before_data.get("name")

    new_name = "Updated Name"
    payload = {"name": new_name}

    response = requests.put(url, headers=headers, json=payload, verify=False)
    assert response.status_code == 200
    assert_is_json(response)

    data = response.json()
    assert data["username"] == user_session["username"]
    assert data["name"] == new_name

    revert_payload = {"name": original_name}
    revert = requests.put(url, headers=headers, json=revert_payload, verify=False)
    assert revert.status_code == 200
    assert_is_json(revert)
    reverted_data = revert.json()
    assert reverted_data["username"] == user_session["username"]
    assert reverted_data.get("name") == original_name


def test_profile_update_with_empty_body_keeps_profile_valid(user_session):
    url = user_session["url"] + "profile"
    headers = {"Authorization": user_session["session_token"]}

    before = requests.get(url, headers=headers, verify=False)
    assert before.status_code == 200
    assert_is_json(before)
    before_data = before.json()

    response = requests.put(url, headers=headers, json={}, verify=False)
    assert response.status_code == 200
    assert_is_json(response)

    after_data = response.json()
    assert after_data["username"] == before_data["username"]
    assert after_data["name"] == before_data["name"]


def test_profile_update_can_change_username(user_session):
    url = user_session["url"] + "profile"
    headers = {"Authorization": user_session["session_token"]}

    before = requests.get(url, headers=headers, verify=False)
    assert before.status_code == 200
    before_data = before.json()
    original_username = before_data["username"]

    new_username = (original_username + "_new").lower()
    payload = {"username": new_username}

    response = requests.put(url, headers=headers, json=payload, verify=False)
    assert response.status_code == 200
    assert_is_json(response)

    data = response.json()
    assert data["username"] == new_username

    revert_payload = {"username": original_username}
    r_revert = requests.put(url, headers=headers, json=revert_payload, verify=False)
    assert r_revert.status_code == 200
    assert_is_json(r_revert)
    reverted = r_revert.json()
    assert reverted["username"] == original_username


def test_profile_put_invalid_json(user_session):
    url = user_session["url"] + "profile"
    headers = {
        "Authorization": user_session["session_token"],
        "Content-Type": "application/json",
    }

    response = requests.put(url, headers=headers, data="INVALID", verify=False)

    assert response.status_code == 400
    assert_is_problem_json(response)