import uuid
import requests

def test_profile_update_requires_authorization(user_session):
    url = user_session["url"] + "profile"
    payload = {"name": "Hacker Name"}
    r = requests.put(url, json=payload)
    assert r.status_code == 401


def test_profile_update_succeeds_with_valid_data(user_session):
    url = user_session["url"] + "profile"
    headers = {"Authorization": user_session["session_token"]}

    new_name = "Updated Name"
    payload = {"name": new_name}

    r = requests.put(url, headers=headers, json=payload)
    assert r.status_code == 200
    assert r.headers.get("Content-Type", "").startswith("application/json")

    data = r.json()
    assert data["username"] == user_session["username"]
    assert data["name"] == new_name


def test_profile_update_with_empty_body_keeps_profile_valid(user_session):
    url = user_session["url"] + "profile"
    headers = {"Authorization": user_session["session_token"]}

    before = requests.get(url, headers=headers)
    assert before.status_code == 200
    before_data = before.json()

    r = requests.put(url, headers=headers, json={})
    assert r.status_code == 200

    after_data = r.json()
    assert after_data["username"] == before_data["username"]
    assert after_data["name"] == before_data["name"]


def test_profile_update_can_change_username(user_session):
    url = user_session["url"] + "profile"
    headers = {"Authorization": user_session["session_token"]}

    new_username = (user_session["username"] + "_new").lower()
    payload = {"username": new_username}

    r = requests.put(url, headers=headers, json=payload)
    assert r.status_code == 200

    data = r.json()
    assert data["username"] == new_username


def test_profile_put_invalid_json(user_session):
    url = user_session["url"] + "profile"
    headers = {"Authorization": user_session["session_token"]}
    data = "INVALID"
    r = requests.put(url, headers=headers, data=data)
    assert r.status_code == 400