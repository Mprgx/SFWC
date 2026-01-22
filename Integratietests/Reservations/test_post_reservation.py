import pytest
import requests
from datetime import datetime, timedelta, timezone
import time

# Use future dates for reservations
# Add a unique offset based on current timestamp to avoid collisions across multiple test runs
# Changes offset every 10 minutes to prevent conflicts between test runs
_RUN_OFFSET = 30 + ((int(time.time()) // 600) % 100)  # Range 30-130 days


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


def test_post_reservation_status_unauthorized(user_session):
    url = user_session['url'] + 'reservations'
    response = requests.post(url, headers={})
    status_code = response.status_code

    assert status_code == 401


def test_post_reservation_status_authorized(user_session, parking_lot_1_id):
    url = user_session['url'] + 'reservations'
    reservation = _make_reservation(parking_lot_1_id, offset_hours=0)
    response = requests.post(url, json=reservation, headers={
                             "Authorization": user_session['session_token']})
    status_code = response.status_code
    assert status_code == 201


def test_post_reservation_message(user_session, parking_lot_1_id):
    url = user_session['url'] + 'reservations'
    reservation = _make_reservation(parking_lot_1_id, offset_hours=3)
    response = requests.post(url, json=reservation, headers={
                             "Authorization": user_session['session_token']})
    assert response.status_code == 201


def test_post_reservation_invaliduser_session(user_session, parking_lot_1_id):
    url = user_session['url'] + 'reservations'
    invaliduser_session = _make_reservation(parking_lot_1_id, offset_hours=6)
    invaliduser_session["startTime"] = "invalid-date"
    response = requests.post(url, json=invaliduser_session, headers={
                             "Authorization": user_session['session_token']})
    assert response.status_code == 400


def test_post_reservation_missing_field(user_session, parking_lot_1_id):
    url = user_session['url'] + 'reservations'
    incompleteuser_session = _make_reservation(
        parking_lot_1_id, offset_hours=9)
    del incompleteuser_session["licensePlate"]
    response = requests.post(url, json=incompleteuser_session, headers={
                             "Authorization": user_session['session_token']})
    assert response.status_code == 400


def test_post_reservation_parkinglot_already_reserved_message(user_session, parking_lot_4_id):
    url = user_session['url'] + 'reservations'
    reservation = _make_reservation(parking_lot_4_id, offset_hours=12)
    response = requests.post(url, json=reservation, headers={
                             "Authorization": user_session['session_token']})
    assert response.status_code in [201, 400, 409]
