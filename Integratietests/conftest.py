import pytest
import requests

BASE_URL = "http://localhost:5280/"

def _register(
    username: str,
    name: str,
    password: str,
    role: int | None = None,
):
    """
    Helper: registers a user via POST /register.
    If the user already exists, we ignore the error.
    """
    url = BASE_URL + "register"

    payload = {
        "username": username,
        "name": name,
        "password": password,
    }

    if role is not None:
        payload["role"] = role  # 0 = Customer, 1 = Admin

    response = requests.post(
        url,
        json=payload,
        timeout=5,
        verify=False,
    )

    # 200 / 201 = success
    # 400 / 409 = user already exists → fine for tests
    if response.status_code not in (200, 201, 400, 409):
        response.raise_for_status()

def _login(username: str, password: str) -> dict:
    """
    Helper: logs in via POST /login and returns a small dict used
    by the tests.
    """
    url = BASE_URL + "login"

    response = requests.post(
        url,
        json={"username": username, "password": password},
        timeout=5,
        verify=False,
    )
    # If login fails, this will make the test fail immediately
    response.raise_for_status()

    data = response.json()
    # Your API returns {"accessToken": "<jwt>", ...}
    raw_token = data["accessToken"]

    return {
        "url": BASE_URL,
        "username": username,
        "accessToken": raw_token,
        "session_token": f"Bearer {raw_token}",
    }

@pytest.fixture(scope="session", autouse=True)
def register_test_users():
    """
    Ensures test users exist before any tests run.
    """
    _register(
        username="usersharad",
        name="User Sharad",
        password="Sharad2002!",
        role=0,  # Customer
    )

    _register(
        username="adminsharad",
        name="Admin Sharad",
        password="Sharad2002!",
        role=1,  # Admin
    )

# -------------------------
# Logged-in user fixtures
# -------------------------

@pytest.fixture
def user_session():
    return _login("usersharad", "Sharad2002!")


@pytest.fixture
def admin_session():
    return _login("adminsharad", "Sharad2002!")


# Aliases for the new parking-lot tests:
# login_as_admin / login_as_user are just the same sessions.

@pytest.fixture
def login_as_admin(admin_session):
    """
    Alias fixture for tests that use `login_as_admin`.
    """
    return admin_session


@pytest.fixture
def login_as_user(user_session):
    """
    Alias fixture for tests that use `login_as_user`.
    """
    return user_session


# -------------------------
# Authorization header fixtures
# -------------------------

@pytest.fixture
def auth_headers(user_session):
    """
    Authorization header for the normal user.
    Used e.g. in Billing and Vehicles tests.
    """
    return {"Authorization": user_session["session_token"]}


@pytest.fixture
def auth_headers_admin(admin_session):
    return {"Authorization": admin_session["session_token"]}


@pytest.fixture
def auth_headers_empty_user():
    """
    Invalid Authorization header for negative/unauthorized tests.
    """
    return {"Authorization": "Bearer emptyusertoken"}
