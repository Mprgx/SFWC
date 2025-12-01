import uuid
import requests

def test_profile_get_unauthorized(user_session):
    url = user_session['url'] + 'profile'
    r = requests.get(url)
    assert r.status_code == 401

def test_get_profile_authorized(user_session):
    url = user_session['url'] + 'profile'
    headers = {"Authorization": user_session['session_token']}
    r = requests.get(url, headers=headers)
    assert r.status_code == 200

def test_profile_get_success(user_session):
    url = user_session["url"] + "profile"
    headers = {"Authorization": user_session["session_token"]}

    r = requests.get(url, headers=headers)
    assert r.status_code == 200
    assert r.json()["username"] == user_session["username"]


def test_profile_get_wrong_token(user_session):
    url = user_session["url"] + "profile"
    headers = {"Authorization": "FAKE123"}
    r = requests.get(url, headers=headers)
    assert r.status_code == 401


def test_profile_get_missing_header(user_session):
    url = user_session["url"] + "profile"
    r = requests.get(url)
    assert r.status_code == 401


def test_profile_get_response_is_json(user_session):
    url = user_session["url"] + "profile"
    headers = {"Authorization": user_session["session_token"]}

    r = requests.get(url, headers=headers)
    assert r.headers.get("Content-Type", "").startswith("application/json")



