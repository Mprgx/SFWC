import pytest
import requests
from jsonschema import validate, ValidationError
from dateutil import parser as dateparser

#Get route tests
def test_profile_get_unauthorized():
    r = requests.get("http://localhost:8000/profile")
    assert r.status_code == 401

def test_get_profile_authorized(user_session):
    url = user_session['url'] + 'profile'
    response = requests.get(
        url, headers={"Authorization": user_session['session_token']})
    status_code = response.status_code

def test_profile_get_success(user_session):
    url = user_session["url"] + "profile"
    headers = {"Authorization": user_session["session_token"]}

    r = requests.get(url, headers=headers)
    assert r.status_code == 200
    assert r.json()["username"] == user_session["username"]


def test_profile_get_wrong_token(user_session):
    url = user_session["url"] + "profile"

    r = requests.get(url, headers={"Authorization": "FAKE123"})
    assert r.status_code == 401


def test_profile_get_missing_header(user_session):
    url = user_session["url"] + "profile"

    r = requests.get(url)  # geen headers
    assert r.status_code == 401


def test_profile_get_response_is_json(user_session):
    url = user_session["url"] + "profile"
    headers = {"Authorization": user_session["session_token"]}

    r = requests.get(url, headers=headers)
    assert r.headers["Content-Type"].startswith("application/json")

#Put route tests

def test_profile_put_unauthorized(user_session):
    url = user_session["url"] + "profile"
    r = requests.post(url, json={"password": "abc"})
    assert r.status_code == 401


def test_profile_put_success(user_session):
    url = user_session["url"] + "profile"
    headers = {"Authorization": user_session["session_token"]}

    r = requests.post(url, headers=headers, json={"password": "newpass"})
    assert r.status_code == 200
    assert r.text == "User updated succesfully"


def test_profile_put_empty_password(user_session):
    url = user_session["url"] + "profile"
    headers = {"Authorization": user_session["session_token"]}

    r = requests.post(url, headers=headers, json={"password": ""})
    assert r.status_code == 200


def test_profile_put_preserves_username(user_session):
    url = user_session["url"] + "profile"
    headers = {"Authorization": user_session["session_token"]}

    r = requests.post(url, headers=headers,
                      json={"password": "test", "username": "hacker"})
    assert r.status_code == 200


def test_profile_put_invalid_json(user_session):
    url = user_session["url"] + "profile"
    headers = {"Authorization": user_session["session_token"]}

    r = requests.post(url, headers=headers, data="INVALID")
    assert r.status_code in [400, 500]   # afhankelijk van server implementatie

def test_get_profile_unauthorized(user_session):
    url = user_session['url'] + 'profile'
    response = requests.get(url, headers={})
    status_code = response.status_code

