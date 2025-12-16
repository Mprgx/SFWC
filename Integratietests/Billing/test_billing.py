import pytest
import requests
import logging

logger = logging.getLogger(__name__)


BASE_PATH = "billing/"


# 1️⃣ Valid token — should return 200 OK with list (possibly empty)
def test_billing_valid_token(auth_headers, user_session):
    """GET /billing with a valid token returns 200"""
    response = requests.get(user_session["url"] + BASE_PATH, headers=auth_headers)
    full_url = user_session["url"] + BASE_PATH
    logger.info("Calling URL: %s", full_url)
    assert response.status_code == 200, f"Expected 200 but got {response.status_code}"
    data = response.json()
    assert isinstance(data, list)


# 2️⃣ Missing token — should return 401 Unauthorized
def test_billing_no_token(user_session):
    """GET /billing without Authorization header"""
    response = requests.get(user_session["url"] + BASE_PATH)
    full_url = user_session["url"] + BASE_PATH
    logger.info("Calling URL: %s", full_url)
    assert response.status_code == 401


# 3️⃣ Invalid token — should also return 401 Unauthorized
def test_billing_invalid_token(user_session):
    """GET /billing with invalid token"""
    headers = {"Authorization": "Bearer invalidtoken123"}
    response = requests.get(user_session["url"] + BASE_PATH, headers=headers)
    full_url = user_session["url"] + BASE_PATH
    logger.info("Calling URL: %s", full_url)
    assert response.status_code == 401


# 4️⃣ Valid token but no sessions — should return 200 with empty list
def test_billing_empty_result(auth_headers_admin, user_session):
    """GET /billing for user with no sessions"""
    response = requests.get(user_session["url"] + BASE_PATH, headers=auth_headers_admin)
    full_url = user_session["url"] + BASE_PATH
    logger.info("Calling URL: %s", full_url)
    assert response.status_code == 200
    data = response.json()
    assert data == [] or len(data) == 0


# 5️⃣ Check numeric consistency (amount = payed + balance)
def test_billing_balance_math(auth_headers, user_session):
    """Ensure /billing balances are correct"""
    response = requests.get(user_session["url"] + BASE_PATH, headers=auth_headers)
    full_url = user_session["url"] + BASE_PATH
    logger.info("Calling URL: %s", full_url)
    if response.status_code != 200:
        pytest.skip(f"Endpoint not available: {response.status_code}")

    data = response.json()

    for record in data:
        amount = record.get("amount", 0)
        payed = record.get("payed", 0)
        balance = record.get("balance", 0)
        # allow small floating point tolerance
        assert abs((amount - payed) - balance) < 0.01
