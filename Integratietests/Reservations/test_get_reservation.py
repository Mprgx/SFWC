import pytest
import requests
from datetime import datetime, timedelta, timezone
import time

RESERVATION_ID = "2"

# Use future dates for reservations
# Add a unique offset based on current timestamp to avoid collisions across multiple test runs
# Changes offset every 10 minutes to prevent conflicts between test runs
_RUN_OFFSET = 30 + ((int(time.time()) // 600) % 100)  # Range 30-130 days

def get_me_profile(session_token, base_url):
    url = base_url + "profile"

    response = requests.get(
        url, headers={"Authorization": session_token})
    
    return response.json()

def _get_unique_future_dates(offset_days=0):
    """Generate unique future dates for reservations to avoid conflicts."""
    # Use _RUN_OFFSET to make each test run use different dates
    # Then use offset_days to separate individual tests
    total_offset_days = _RUN_OFFSET + offset_days
    future_date = datetime.now(
        timezone.utc) + timedelta(days=total_offset_days)
    future_date_str = future_date.replace(
        hour=11, minute=0, second=0, microsecond=0).isoformat()
    end_date_str = (future_date + timedelta(days=1)).replace(hour=14,
                                                             minute=0, second=0, microsecond=0).isoformat()
    return future_date_str, end_date_str


def _make_reservation(parking_lot_id: int, license_plate: str = "AB-123-CD", offset_hours=0):
    """Helper to create a reservation dict with the given parking lot ID."""
    # offset_hours is in days (legacy parameter name, now in days)
    start_time, end_time = _get_unique_future_dates(offset_hours)
    return {
        "parkingLotId": parking_lot_id,
        "licensePlate": license_plate,
        "startTime": start_time,
        "endTime": end_time,
    }


def test_get_reservation_status_unauthorized(user_session):
    url = user_session['url'] + 'reservations/by-id/' + RESERVATION_ID
    response = requests.get(url, headers={})
    status_code = response.status_code

    assert status_code == 401


def test_get_reservation_status_authorized(user_session):
    # Arrange: create reservation
    payload = _make_reservation(parking_lot_id=1)
    post = requests.post(
        user_session["url"] + "reservations",
        json=payload,
        headers={"Authorization": user_session["session_token"]}
    )
    assert post.status_code == 201
    reservation_id = post.json()["id"]

    # Act
    url = user_session["url"] + f"reservations/by-id/{reservation_id}"
    response = requests.get(
        url,
        headers={"Authorization": user_session["session_token"]}
    )

    # Assert
    assert response.status_code == 200




def test_get_reservation_correct_message(user_session):
    payload = _make_reservation(parking_lot_id=2)
    post = requests.post(
        user_session["url"] + "reservations",
        json=payload,
        headers={"Authorization": user_session["session_token"]}
    )
    print(post.json())
    reservation_id = post.json()["id"]

    response = requests.get(
        user_session["url"] + f"reservations/by-id/{reservation_id}",
        headers={"Authorization": user_session["session_token"]}
    )

    data = response.json()
    assert response.status_code == 200
    assert data["isActive"] is True



def test_get_reservation_invalid_id(user_session):
    invalid_reservation_id = "9999"
    url = user_session['url'] + 'reservations/by-id/' + invalid_reservation_id
    response = requests.get(
        url, headers={"Authorization": user_session['session_token']})
    assert response.status_code == 404


def test_get_reservation_invalid_id_message(user_session):
    invalid_reservation_id = "9999"
    url = user_session['url'] + 'reservations/by-id/' + invalid_reservation_id
    response = requests.get(
        url, headers={"Authorization": user_session['session_token']})
    assert response.status_code == 404
    response_data = response.json()
    assert 'message' in response_data
    assert 'does not exist' in response_data['message'].lower(
    ) or invalid_reservation_id in response_data['message']


def test_get_reservation_verify_ownership(user_session):
    payload = _make_reservation(parking_lot_id=2, license_plate="AB-123-CD", offset_hours=3)
    post = requests.post(
        user_session["url"] + "reservations",
        json=payload,
        headers={"Authorization": user_session["session_token"]}
    )
    reservation_id = post.json()["id"]

    response = requests.get(
        user_session["url"] + f"reservations/by-id/{reservation_id}",
        headers={"Authorization": user_session["session_token"]}
    )

    user_id = get_me_profile(user_session["session_token"], "http://localhost:5280/")["id"]

    assert response.status_code == 200
    print(user_session)
    print(response.json())
    assert response.json()["userId"] == user_id
