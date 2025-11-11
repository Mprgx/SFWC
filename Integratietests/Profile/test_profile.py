import pytest
import requests


def test_get_profile_authorized(user_session):
    url = user_session['url'] + 'profile'
    response = requests.get(
        url, headers={"Authorization": user_session['session_token']})
    status_code = response.status_code

    assert status_code == 200


def test_get_profile_unauthorized(user_session):
    url = user_session['url'] + 'profile'
    response = requests.get(url, headers={})
    status_code = response.status_code

    assert status_code == 401
