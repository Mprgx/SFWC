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
        print(response.status_code)
        print(response.text)
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


def _create_parking_lot(admin_session, name: str, location: str, capacity: int):
    """
    Helper: creates a parking lot via POST /parking-lots.
    Returns the parking lot data if successful.
    """
    url = BASE_URL + "parking-lots"

    payload = {
        "name": name,
        "location": location,
        "address": "Test Address",
        "capacity": capacity,
        "tariff": 1.5,
        "dayTariff": 10.0,
        "coordinates": {"latitude": 52.0, "longitude": 5.1}
    }

    headers = {"Authorization": admin_session["session_token"]}

    response = requests.post(
        url,
        json=payload,
        headers=headers,
        timeout=5,
        verify=False,
    )

    if response.status_code == 201:
        return response.json()
    else:
        print(f"Failed to create parking lot: {response.status_code}")
        print(response.text)
        return None


def _create_vehicle(user_session, license_plate: str):
    """
    Helper: creates a vehicle via POST /vehicle.
    Returns the vehicle data if successful.
    """
    url = BASE_URL + "vehicle"

    payload = {
        "licensePlate": license_plate,
        "make": "Tesla",
        "model": "Model S",
        "color": "Black",
        "year": 2022,
    }

    headers = {"Authorization": user_session["session_token"]}

    response = requests.post(
        url,
        json=payload,
        headers=headers,
        timeout=5,
        verify=False,
    )

    if response.status_code in (200, 201):
        return response.json()
    else:
        print(f"Failed to create vehicle: {response.status_code}")
        print(response.text)
        return None

# -------------------------
# Logged-in user fixtures
# -------------------------


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


@pytest.fixture(scope="session", autouse=True)
def setup_parking_lots(register_test_users):
    """
    Creates parking lots needed for integration tests.
    This runs after register_test_users.
    """
    admin = _login("adminsharad", "Sharad2002!")

    # Create parking lots for reservations tests
    _create_parking_lot(admin, "TestLot1", "Center", 100)
    _create_parking_lot(admin, "TestLot4", "South", 50)


@pytest.fixture(scope="session")
def parking_lot_1_id(admin_session):
    """
    Returns the ID of the first test parking lot (TestLot1).
    """
    # Get list of parking lots and find TestLot1
    url = BASE_URL + "parking-lots"
    headers = {"Authorization": admin_session["session_token"]}
    response = requests.get(url, headers=headers)
    if response.status_code == 200:
        lots = response.json()
        for lot in reversed(lots):  # Get the most recently created one
            if lot['name'] == 'TestLot1':
                return lot['id']
    # Fallback: try ID 1
    return 1


@pytest.fixture(scope="session")
def parking_lot_4_id(admin_session):
    """
    Returns the ID of the second test parking lot (TestLot4).
    """
    # Get list of parking lots and find TestLot4
    url = BASE_URL + "parking-lots"
    headers = {"Authorization": admin_session["session_token"]}
    response = requests.get(url, headers=headers)
    if response.status_code == 200:
        lots = response.json()
        for lot in reversed(lots):  # Get the most recently created one
            if lot['name'] == 'TestLot4':
                return lot['id']
    # Fallback: try ID 4
    return 4


@pytest.fixture(scope="session", autouse=True)
def setup_test_vehicle(setup_parking_lots):
    """
    Creates a test vehicle for the test user.
    This is needed for reservation tests.
    This runs after setup_parking_lots to ensure proper ordering.
    """
    user = _login("usersharad", "Sharad2002!")

    # Create a vehicle with the license plate used in reservation tests
    _create_vehicle(user, "AB-123-CD")


@pytest.fixture(scope="session")
def user_session():
    return _login("usersharad", "Sharad2002!")


@pytest.fixture(scope="session")
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
