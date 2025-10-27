import requests
import pytest

BASE_PATH = "billing/"

# 1️⃣ GET /billings - authorized
def test_get_billings_authorized(auth_headers, _data):
    response = requests.get(_data["url"] + BASE_PATH, headers=auth_headers)
    assert response.status_code == 200

# 2️⃣ GET /billings - unauthorized
def test_get_billings_unauthorized(_data):
    response = requests.get(_data["url"] + BASE_PATH)
    assert response.status_code == 401

def test_get_billing_correctly(auth_headers, _data):
    response = requests.get(_data["url"] + BASE_PATH, headers=auth_headers)
    