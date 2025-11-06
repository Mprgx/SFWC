import requests
import pytest

BASE_PATH = "billing/"

# 1️⃣ GET /billings - authorized


def test_get_billings_authorized(auth_headers, user_session):
    response = requests.get(
        user_session["url"] + BASE_PATH, headers=auth_headers)
    assert response.status_code == 200

# 2️⃣ GET /billings - unauthorized


def test_get_billings_unauthorized(user_session):
    response = requests.get(user_session["url"] + BASE_PATH)
    assert response.status_code == 401


def test_get_billing_correctly(auth_headers, user_session):
    response = requests.get(
        user_session["url"] + BASE_PATH, headers=auth_headers)
