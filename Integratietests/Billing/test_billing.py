import pytest
import requests

BASE_PATH = "billing/"


# 1️⃣ Valid token — should return 200 OK with list (possibly empty)
def test_billing_valid_token(auth_headers, _data):
    """GET /billing with a valid token returns 200"""
    response = requests.get(_data["url"] + BASE_PATH, headers=auth_headers)
    assert response.status_code == 200, f"Expected 200 but got {response.status_code}"
    data = response.json()
    assert isinstance(data, list)
    # optional structure check
    if data:
        record = data[0]
        assert "session" in record
        assert "parking" in record
        assert "amount" in record
        assert "balance" in record


# 2️⃣ Missing token — should return 401 Unauthorized
def test_billing_no_token(_data):
    """GET /billing without Authorization header"""
    response = requests.get(_data["url"] + BASE_PATH)
    assert response.status_code == 401


# 3️⃣ Invalid token — should also return 401 Unauthorized
def test_billing_invalid_token(_data):
    """GET /billing with invalid token"""
    headers = {"Authorization": "Bearer invalidtoken123"}
    response = requests.get(_data["url"] + BASE_PATH, headers=headers)
    assert response.status_code == 401


# 4️⃣ Valid token but no sessions — should return 200 with empty list
def test_billing_empty_result(auth_headers_empty_user, _data):
    """GET /billing for user with no sessions"""
    response = requests.get(_data["url"] + BASE_PATH, headers=auth_headers_empty_user)
    assert response.status_code == 200
    data = response.json()
    assert data == [] or len(data) == 0


# 5️⃣ Check numeric consistency (amount = payed + balance)
def test_billing_balance_math(auth_headers, _data):
    """Ensure /billing balances are correct"""
    response = requests.get(_data["url"] + BASE_PATH, headers=auth_headers)
    if response.status_code != 200:
        pytest.skip(f"Endpoint not available: {response.status_code}")

    data = response.json()
    if not data:
        pytest.skip("No billing data to validate.")

    for record in data:
        amount = record.get("amount", 0)
        payed = record.get("payed", 0)
        balance = record.get("balance", 0)
        # allow small floating point tolerance
        assert abs((amount - payed) - balance) < 0.01
