import uuid
import requests

def assert_is_json(response: requests.Response) -> None:
    assert response.headers.get("Content-Type", "").startswith("application/json")

def test_profile_get_unauthorized(user_session):
    url = user_session['url'] + 'profile'

    response = requests.get(url, verify=False)

    assert response.status_code == 401

def test_get_profile_authorized(user_session):
    url = user_session['url'] + 'profile'
    headers = {"Authorization": user_session['session_token']}

    response = requests.get(url, headers=headers, verify=False)

    assert response.status_code == 200

def test_profile_get_success(user_session):
    url = user_session["url"] + "profile"
    headers = {"Authorization": user_session["session_token"]}

    response = requests.get(url, headers=headers, verify=False)
    
    assert response.status_code == 200
    assert_is_json(response)

    
    assert response.json()["username"] == user_session["username"]


def test_profile_get_wrong_token(user_session):
    url = user_session["url"] + "profile"
    headers = {"Authorization": "FAKE123"}

    r = requests.get(url, headers=headers, verify=False)

    assert r.status_code == 401


def test_profile_get_missing_header(user_session):
    url = user_session["url"] + "profile"

    r = requests.get(url, verify=False)

    assert r.status_code == 401


def test_profile_get_response_is_json(user_session):
    url = user_session["url"] + "profile"
    headers = {"Authorization": user_session["session_token"]}

    response = requests.get(url, headers=headers, verify=False)

    assert_is_json(response)



