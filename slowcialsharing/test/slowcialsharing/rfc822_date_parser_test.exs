defmodule RFC822DateParserTest do
  use ExUnit.Case
  doctest RFC822DateParser

  describe "parse_date/1" do
    test "parses valid RFC-822 date with UTC offset" do
      assert RFC822DateParser.parse_date("Mon, 07 Nov 1994 08:49:37 +0000") ==
               {:ok, ~U[1994-11-07 08:49:37Z]}
    end

    test "parses valid RFC-822 date with positive offset" do
      assert RFC822DateParser.parse_date("07 Nov 1994 10:49:37 +0200") ==
               {:ok, ~U[1994-11-07 08:49:37Z]}
    end

    test "parses valid RFC-822 date with negative offset" do
      assert RFC822DateParser.parse_date("07 Nov 1994 00:49:37 -0800") ==
               {:ok, ~U[1994-11-07 08:49:37Z]}
    end

    test "handles dates without day name" do
      assert RFC822DateParser.parse_date("07 Nov 1994 08:49:37 +0000") ==
               {:ok, ~U[1994-11-07 08:49:37Z]}
    end

    test "handles two-digit years in the 2000s" do
      assert {:ok, dt} = RFC822DateParser.parse_date("07 Nov 23 08:49:37 +0000")
      assert dt.year == 2023
    end

    test "handles two-digit years in the 1900s" do
      assert {:ok, dt} = RFC822DateParser.parse_date("07 Nov 94 08:49:37 +0000")
      assert dt.year == 1994
    end

    test "handles single-digit days" do
      assert {:ok, dt} = RFC822DateParser.parse_date("7 Nov 2023 08:49:37 +0000")
      assert dt.day == 7
    end

    test "returns error for invalid month" do
      assert RFC822DateParser.parse_date("07 Nev 2023 08:49:37 +0000") ==
               {:error, :invalid_month}
    end

    test "returns error for invalid format" do
      assert RFC822DateParser.parse_date("not a date") ==
               {:error, :invalid_format}
    end

    test "returns error for invalid timezone format" do
      assert RFC822DateParser.parse_date("07 Nov 2023 08:49:37 GMT") ==
               {:error, :invalid_format}
    end

    test "returns error for invalid input type" do
      assert RFC822DateParser.parse_date(nil) ==
               {:error, :invalid_input}
    end

    test "returns error for invalid date components" do
      assert RFC822DateParser.parse_date("31 Feb 2023 08:49:37 +0000") ==
               {:error, :invalid_datetime}
    end

    test "returns error for invalid time components" do
      assert RFC822DateParser.parse_date("07 Nov 2023 25:49:37 +0000") ==
               {:error, :invalid_datetime}
    end

    test "handles dates at DST boundaries correctly" do
      # Testing a time during DST transition doesn't depend on timezone
      # since we're only handling explicit offsets
      assert {:ok, _} = RFC822DateParser.parse_date("31 Oct 2023 01:30:00 +0100")
    end

    test "handles leap year dates correctly" do
      assert {:ok, dt} = RFC822DateParser.parse_date("29 Feb 2020 12:00:00 +0000")
      assert dt.day == 29
      assert dt.month == 2
      assert dt.year == 2020
    end

    test "returns error for invalid leap year date" do
      assert RFC822DateParser.parse_date("29 Feb 2023 12:00:00 +0000") ==
               {:error, :invalid_datetime}
    end
  end
end
