import uuid
import requests
import pytest


def assert_is_json(response: requests.Response) -> None:
    assert response.headers.get(
        "Content-Type", "").startswith("application/json")


def test_get_my_payments(user_session):
    url = user_session["url"] + "payments"
    headers = {"Authorization": user_session["session_token"]}

    response = requests.get(url, headers=headers, verify=False)

    assert response.status_code == 200
    assert_is_json(response)
    assert isinstance(response.json(), list)


def test_admin_get_other_user_payments(admin_session):
    target_username = "usersharad"
    url = admin_session["url"] + f"payments/user/{target_username}"
    headers = {"Authorization": admin_session["session_token"]}

    response = requests.get(url, headers=headers, verify=False)

    assert response.status_code == 200
    assert isinstance(response.json(), list)


def test_admin_get_payments_user_not_found(admin_session):
    target_username = "bestaatniet"
    url = admin_session["url"] + f"payments/user/{target_username}"
    headers = {"Authorization": admin_session["session_token"]}

    response = requests.get(url, headers=headers, verify=False)

    assert response.status_code == 404
