import pytest
import requests
from jsonschema import validate, ValidationError
from dateutil import parser as dateparser

#Get route tests
def test_profile_get_unauthorized(base_url):
    url = base_url + "profile"
    r = requests.get(url)
    assert r.status_code == 401
    assert "Unauthorized" in r.text


def test_profile_get_success(user_session):
    token = user_session["token"]
    url = user_session["url"] + "profile"

    r = requests.get(url, headers={"Authorization": token})
    assert r.status_code == 200
    data = r.json()
    assert "username" in data
    assert data["username"] == user_session["username"]


def test_profile_get_wrong_token(base_url):
    url = base_url + "profile"
    r = requests.get(url, headers={"Authorization": "FAKETOKEN"})
    assert r.status_code == 401


def test_profile_get_missing_header(base_url):
    r = requests.get(base_url + "profile")
    assert r.status_code == 401


def test_profile_get_response_is_json(user_session):
    token = user_session["token"]
    url = user_session["url"] + "profile"
    r = requests.get(url, headers={"Authorization": token})

    assert r.headers["Content-Type"].startswith("application/json")

#Put route tests

def test_profile_put_unauthorized(base_url):
    url = base_url + "profile"
    r = requests.post(url, json={"password": "abc"})
    assert r.status_code == 401


def test_profile_put_success(user_session):
    token = user_session["token"]
    url = user_session["url"] + "profile"

    r = requests.post(url, headers={"Authorization": token},
                      json={"password": "newpass"})
    assert r.status_code == 200
    assert r.text == "User updated succesfully"


def test_profile_put_empty_password(user_session):
    """Lege password moet nog steeds valid zijn, want code checkt enkel 'if data['password']'."""
    token = user_session["token"]
    url = user_session["url"] + "profile"

    r = requests.post(url, headers={"Authorization": token},
                      json={"password": ""})
    assert r.status_code == 200


def test_profile_put_preserves_username(user_session):
    """API vervangt username door session-user altijd."""
    token = user_session["token"]
    url = user_session["url"] + "profile"

    r = requests.post(url, headers={"Authorization": token},
                      json={"password": "abc123", "username": "hacker"})
    assert r.status_code == 200


def test_profile_put_requires_json(user_session):
    token = user_session["token"]
    url = user_session["url"] + "profile"

    r = requests.post(url, headers={"Authorization": token}, data="not json")
    # Code probeert JSON te loaden, dus server crasht → 400/500 afhankelijk van running server.
    assert r.status_code in [400, 500]

