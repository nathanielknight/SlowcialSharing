defmodule Slowcialsharing.Item do
  use Ecto.Schema
  import Ecto.Changeset

  schema "items" do
    field :link, :string
    field :title, :string
    field :key, :string
    field :comments, :integer
    field :pubdate, :utc_datetime
    field :commentslink, :string
    field :score, :integer
    field :site_id, :id

    timestamps(type: :utc_datetime)
  end


  @doc false
  def changeset(item, attrs) do
    item
    |> cast(attrs, [:key, :title, :link, :pubdate, :commentslink, :score, :comments])
    |> validate_required([:key, :title, :link, :pubdate, :commentslink, :comments])
    |> unique_constraint(:key)
  end
end
