defmodule Slowcialsharing.Site do
  use Ecto.Schema
  import Ecto.Changeset

  schema "sites" do
    field :name, :string
    field :homepageurl, :string
    field :lastchecked, :utc_datetime

    timestamps(type: :utc_datetime)
  end

  @doc false
  def changeset(sites, attrs) do
    sites
    |> cast(attrs, [:name, :homepageurl, :lastchecked])
    |> validate_required([:name, :homepageurl])
  end
end
