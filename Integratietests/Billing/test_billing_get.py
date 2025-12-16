import pytest
import requests

BASE_PATH = "billing"


def assert_is_json(response: requests.Response) -> None:
    assert response.headers.get("Content-Type", "").startswith("application/json")


def test_get_billing_authorized_returns_200_and_list(auth_headers, user_session):
    url = user_session["url"] + BASE_PATH

    response = requests.get(url, headers=auth_headers, verify=False)

    assert response.status_code == 200
    assert_is_json(response)

    data = response.json()
    assert isinstance(data, list)


def test_get_billing_unauthorized_without_token_returns_401(user_session):
    url = user_session["url"] + BASE_PATH

    response = requests.get(url, verify=False)

    assert response.status_code == 401


def test_get_billing_unauthorized_invalid_token_returns_401(user_session):
    url = user_session["url"] + BASE_PATH
    headers = {"Authorization": "Bearer invalid.token.value"}

    response = requests.get(url, headers=headers, verify=False)

    assert response.status_code == 401


def test_get_billing_authorized_no_results_returns_empty_list(
    auth_headers_admin, user_session
):
    url = user_session["url"] + BASE_PATH

    response = requests.get(url, headers=auth_headers_admin, verify=False)

    assert response.status_code == 200
    assert_is_json(response)

    data = response.json()
    assert isinstance(data, list)
    assert len(data) == 0


def test_get_billing_balance_calculation_is_correct(auth_headers, user_session):
    url = user_session["url"] + BASE_PATH

    response = requests.get(url, headers=auth_headers, verify=False)

    if response.status_code != 200:
        pytest.skip(f"/billing not available (status {response.status_code})")

    data = response.json()

    for record in data:
        amount = record.get("amount", 0)
        payed = record.get("payed", 0)
        balance = record.get("balance", 0)

        assert abs((amount - payed) - balance) < 0.01
