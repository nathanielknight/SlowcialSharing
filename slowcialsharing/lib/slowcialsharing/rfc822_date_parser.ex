defmodule RFC822DateParser do
  @moduledoc """
  Functions for parsing dates in RFC-822 format.
  """

  @doc """
  Parses a date in RFC-822 format and returns a DateTime struct.
  Only handles numeric timezone offsets (e.g., +0100, -0500).

  ## Examples

      iex> RFC822DateParser.parse_date("Mon, 07 Nov 1994 08:49:37 +0000")
      {:ok, ~U[1994-11-07 08:49:37Z]}

      iex> RFC822DateParser.parse_date("Mon, 07 Nov 1994 08:49:37 -0800")
      {:ok, ~U[1994-11-07 16:49:37Z]}

      iex> RFC822DateParser.parse_date("invalid date")
      {:error, :invalid_format}

  """
  @spec parse_date(String.t()) :: {:ok, DateTime.t()} | {:error, atom()}
  def parse_date(date_string) when is_binary(date_string) do
    # Regex to match RFC-822 date format with numeric timezone offsets
    # Group 1: day name (optional)
    # Group 2: day
    # Group 3: month
    # Group 4: year
    # Group 5: hour
    # Group 6: minute
    # Group 7: second
    # Group 8: timezone (only numeric offsets +/-HHMM)
    regex =
      ~r/(?:([A-Za-z]{3}),\s+)?(\d{1,2})\s+([A-Za-z]{3})\s+(\d{2,4})\s+(\d{2}):(\d{2}):(\d{2})\s+([+-]\d{4})/

    case Regex.run(regex, date_string) do
      [_, _day_name, day, month, year, hour, minute, second, timezone] ->
        with {:ok, month_num} <- month_to_number(month),
             year_num = normalize_year(year),
             {:ok, tz_offset} <- parse_timezone(timezone),
             {:ok, naive_dt} <-
               build_naive_datetime(year_num, month_num, day, hour, minute, second),
             {:ok, dt_with_tz} <- apply_timezone(naive_dt, tz_offset) do
          {:ok, dt_with_tz}
        else
          {:error, reason} -> {:error, reason}
        end

      _ ->
        {:error, :invalid_format}
    end
  end

  def parse_date(_), do: {:error, :invalid_input}

  # Converts month abbreviation to its number
  defp month_to_number(month) do
    months = %{
      "Jan" => 1,
      "Feb" => 2,
      "Mar" => 3,
      "Apr" => 4,
      "May" => 5,
      "Jun" => 6,
      "Jul" => 7,
      "Aug" => 8,
      "Sep" => 9,
      "Oct" => 10,
      "Nov" => 11,
      "Dec" => 12
    }

    case Map.get(months, String.capitalize(String.slice(month, 0, 3))) do
      nil -> {:error, :invalid_month}
      num -> {:ok, num}
    end
  end

  # Normalizes two-digit years to four digits
  defp normalize_year(year) do
    year_num = String.to_integer(year)

    cond do
      year_num < 50 -> 2000 + year_num
      year_num < 100 -> 1900 + year_num
      true -> year_num
    end
  end

  # Parses numeric timezone offset into seconds
  defp parse_timezone(timezone) do
    # Handle numeric timezone offsets like +0100 or -0500
    if String.match?(timezone, ~r/^[+-]\d{4}$/) do
      hours = String.to_integer(String.slice(timezone, 1, 2))
      minutes = String.to_integer(String.slice(timezone, 3, 2))
      sign = if String.starts_with?(timezone, "+"), do: 1, else: -1
      {:ok, sign * (hours * 3600 + minutes * 60)}
    else
      {:error, :invalid_timezone}
    end
  end

  # Builds a NaiveDateTime from components
  defp build_naive_datetime(year, month, day, hour, minute, second) do
    with {:ok, date} <- Date.new(year, month, String.to_integer(day)),
         {:ok, time} <-
           Time.new(String.to_integer(hour), String.to_integer(minute), String.to_integer(second)) do
      NaiveDateTime.new(date, time)
    else
      {:error, _} -> {:error, :invalid_datetime}
    end
  end

  # Applies timezone offset and converts to UTC DateTime
  defp apply_timezone(naive_dt, tz_offset) do
    with {:ok, dt} <- DateTime.from_naive(naive_dt, "Etc/UTC") do
      dt = DateTime.shift(dt, Duration.new!(second: -tz_offset))
      {:ok, dt}
    end
  end
end
